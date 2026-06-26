// 담당자 : 정승우
// 설명   : Firestore 저장/로드 서비스
// 2차 수정자 : 조규민
// 수정 내용 : FirebaseFirestore 필드 복구, UID/DB 방어, 로컬 백업 실패 방어 추가
// 3차 수정자 : 조규민
// 수정 내용 : UserCloudData 직접 저장 대신 Firestore Dictionary 변환 저장으로 직렬화 오류 수정

#if FIREBASE_ENABLED
using Firebase.Firestore;
#endif
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
// 추가: 조규민 - 계정별 로컬 백업 분리와 Firestore 저장 타임아웃 방어 추가
#if FIREBASE_ENABLED
using System.Threading.Tasks;
#endif
using UnityEngine;

public class FirestoreService : MonoBehaviour
{
    public event Action<UserCloudData> OnDataLoaded;
    public event Action OnSaveComplete;
    public event Action<string> OnError;
    public string LastError { get; private set; }

    private string _uid;
    private const string LOCAL_BACKUP_FILE = "cloud_backup.json";
    private const float FIRESTORE_TIMEOUT_SECONDS = 15f;

#if FIREBASE_ENABLED
    private FirebaseFirestore _db; // 추가: 조규민 - FIREBASE_ENABLED 컴파일 시 사용하는 Firestore 인스턴스
#endif

    public void Initialize(string uid)
    {
        _uid = uid;

#if FIREBASE_ENABLED
        // 추가: 조규민 - 인증 성공 후 UID를 받은 시점에 Firestore 인스턴스를 준비한다.
        _db = FirebaseFirestore.DefaultInstance;
#endif
    }

    public IEnumerator SaveCoroutine(UserCloudData data)
    {
        LastError = null;

        if (data == null)
        {
            SetError("저장할 유저 데이터가 없습니다.");
            yield break;
        }

        if (string.IsNullOrEmpty(_uid))
        {
            SaveLocalBackup(data);
            yield break;
        }

        data.lastLoginAt = DateTime.UtcNow.ToString("o");
#if FIREBASE_ENABLED
        if (_db == null)
        {
            SaveLocalBackup(data);
            SetError("Firestore가 초기화되지 않았습니다.");
            yield break;
        }

        var docRef = _db.Collection("users").Document(_uid);
        var task = docRef.SetAsync(CreateUserCloudDataDictionary(data)); // 추가: 조규민 - Firestore SDK가 사용자 클래스를 직접 변환하지 못해 Dictionary로 변환해 저장한다.
        bool saveCompleted = false;
        yield return StartCoroutine(WaitForFirestoreTask(task, "Firestore 저장 시간이 초과되었습니다.", result => saveCompleted = result));
        if (!saveCompleted)
        {
            SaveLocalBackup(data);
            yield break;
        }

        if (task.IsFaulted)
        {
            SaveLocalBackup(data);
            SetError(GetExceptionMessage(task.Exception, "저장 실패"));
            yield break;
        }
#endif
        SaveLocalBackup(data);
        OnSaveComplete?.Invoke();
    }

    public IEnumerator LoadCoroutine()
    {
        if (string.IsNullOrEmpty(_uid))
        {
            OnDataLoaded?.Invoke(LoadLocalBackup() ?? UserCloudData.CreateDefault());
            yield break;
        }
#if FIREBASE_ENABLED
        if (_db == null)
        {
            OnError?.Invoke("Firestore가 초기화되지 않았습니다.");
            OnDataLoaded?.Invoke(LoadLocalBackup() ?? UserCloudData.CreateDefault());
            yield break;
        }

        var docRef = _db.Collection("users").Document(_uid);
        var task = docRef.GetSnapshotAsync();
        yield return new WaitUntil(() => task.IsCompleted);
        if (task.IsFaulted)
        {
            OnError?.Invoke(GetExceptionMessage(task.Exception, "로드 실패"));
            OnDataLoaded?.Invoke(LoadLocalBackup() ?? UserCloudData.CreateDefault());
            yield break;
        }

        if (task.Result.Exists)
        {
            var data = ConvertSnapshotToUserCloudData(task.Result);
            SaveLocalBackup(data);
            OnDataLoaded?.Invoke(data);
        }
        else
        {
            var newData = UserCloudData.CreateDefault();
            newData.uid = _uid;
            yield return StartCoroutine(SaveCoroutine(newData));
            OnDataLoaded?.Invoke(newData);
        }
#else
        var localData = LoadLocalBackup() ?? UserCloudData.CreateDefault();
        SaveLocalBackup(localData);
        OnDataLoaded?.Invoke(localData);
        yield return null;
#endif
    }

    public IEnumerator CheckUserProfileExistsCoroutine(Action<bool> onCompleted)
    {
        if (string.IsNullOrEmpty(_uid))
        {
            onCompleted?.Invoke(false);
            yield break;
        }

#if FIREBASE_ENABLED
        if (_db == null)
        {
            OnError?.Invoke("Firestore가 초기화되지 않았습니다.");
            onCompleted?.Invoke(false);
            yield break;
        }

        var task = _db.Collection("users").Document(_uid).GetSnapshotAsync();
        bool profileCheckCompleted = false;
        yield return StartCoroutine(WaitForFirestoreTask(task, "회원 프로필 확인 시간이 초과되었습니다.", result => profileCheckCompleted = result));
        if (!profileCheckCompleted)
        {
            onCompleted?.Invoke(false);
            yield break;
        }

        if (task.IsCanceled || task.IsFaulted)
        {
            OnError?.Invoke(GetExceptionMessage(task.Exception, "유저 프로필 확인 실패"));
            onCompleted?.Invoke(false);
            yield break;
        }

        if (!task.Result.Exists)
        {
            onCompleted?.Invoke(false);
            yield break;
        }

        bool hasCompletedProfile = false;
        if (task.Result.TryGetValue("playerId", out string playerId))
        {
            hasCompletedProfile = !string.IsNullOrWhiteSpace(playerId);
        }

        onCompleted?.Invoke(hasCompletedProfile);
#else
        onCompleted?.Invoke(HasLocalGoogleProfileForCurrentUser());
        yield return null;
#endif
    }

    public IEnumerator CreateGoogleUserProfileCoroutine(string playerId, string email, string displayName, Action<bool> onCompleted)
    {
        LastError = null;

        if (string.IsNullOrEmpty(_uid))
        {
            SetError("회원가입할 UID가 없습니다.");
            onCompleted?.Invoke(false);
            yield break;
        }

        if (string.IsNullOrEmpty(playerId))
        {
            SetError("아이디를 입력해 주세요.");
            onCompleted?.Invoke(false);
            yield break;
        }

#if FIREBASE_ENABLED
        if (_db == null)
        {
            SetError("Firestore가 초기화되지 않았습니다.");
            onCompleted?.Invoke(false);
            yield break;
        }

        string normalizedPlayerId = playerId.Trim().ToLowerInvariant();
        var playerIdRef = _db.Collection("playerIds").Document(normalizedPlayerId);
        var playerIdTask = playerIdRef.GetSnapshotAsync();
        bool playerIdCheckCompleted = false;
        yield return StartCoroutine(WaitForFirestoreTask(playerIdTask, "아이디 중복 확인 시간이 초과되었습니다.", result => playerIdCheckCompleted = result));
        if (!playerIdCheckCompleted)
        {
            onCompleted?.Invoke(false);
            yield break;
        }

        if (playerIdTask.IsCanceled || playerIdTask.IsFaulted)
        {
            SetError(GetExceptionMessage(playerIdTask.Exception, "아이디 중복 확인 실패"));
            onCompleted?.Invoke(false);
            yield break;
        }

        if (playerIdTask.Result.Exists)
        {
            SetError("이미 사용 중인 아이디입니다.");
            onCompleted?.Invoke(false);
            yield break;
        }

        var userData = CreateGoogleUserCloudData(normalizedPlayerId, email, displayName);
        var userTask = _db.Collection("users").Document(_uid).SetAsync(CreateGoogleUserDictionary(userData));
        bool userSaveCompleted = false;
        yield return StartCoroutine(WaitForFirestoreTask(userTask, "회원가입 저장 시간이 초과되었습니다.", result => userSaveCompleted = result));
        if (!userSaveCompleted)
        {
            onCompleted?.Invoke(false);
            yield break;
        }

        if (userTask.IsCanceled || userTask.IsFaulted)
        {
            SetError(GetExceptionMessage(userTask.Exception, "회원가입 저장 실패"));
            onCompleted?.Invoke(false);
            yield break;
        }

        var playerIdData = new Dictionary<string, object>
        {
            { "uid", _uid },
            { "createdAt", DateTime.UtcNow.ToString("o") }
        };
        var reserveTask = playerIdRef.SetAsync(playerIdData);
        bool reserveCompleted = false;
        yield return StartCoroutine(WaitForFirestoreTask(reserveTask, "아이디 예약 시간이 초과되었습니다.", result => reserveCompleted = result));
        if (!reserveCompleted)
        {
            onCompleted?.Invoke(false);
            yield break;
        }

        if (reserveTask.IsCanceled || reserveTask.IsFaulted)
        {
            SetError(GetExceptionMessage(reserveTask.Exception, "아이디 예약 실패"));
            onCompleted?.Invoke(false);
            yield break;
        }

        SaveLocalBackup(userData);
#else
        SetError("Firebase가 비활성화되어 실제 회원가입을 저장할 수 없습니다. Android 빌드에서 FIREBASE_ENABLED 상태로 실행해 주세요.");
        onCompleted?.Invoke(false);
        yield return null;
        yield break;
#endif
        onCompleted?.Invoke(true);
    }

    public IEnumerator CreateAutomaticGoogleUserProfileCoroutine(string playerId, string email, string displayName, Action<bool> onCompleted)
    {
        LastError = null;

        if (string.IsNullOrEmpty(_uid))
        {
            SetError("회원가입할 UID가 없습니다.");
            onCompleted?.Invoke(false);
            yield break;
        }

        if (string.IsNullOrEmpty(playerId))
        {
            SetError("자동 생성 아이디가 비어 있습니다.");
            onCompleted?.Invoke(false);
            yield break;
        }

#if FIREBASE_ENABLED
        if (_db == null)
        {
            SetError("Firestore가 초기화되지 않았습니다.");
            onCompleted?.Invoke(false);
            yield break;
        }

        var userData = CreateGoogleUserCloudData(playerId.Trim().ToLowerInvariant(), email, displayName);
        var userTask = _db.Collection("users").Document(_uid).SetAsync(CreateGoogleUserDictionary(userData));
        bool userSaveCompleted = false;
        yield return StartCoroutine(WaitForFirestoreTask(userTask, "자동 회원가입 저장 시간이 초과되었습니다.", result => userSaveCompleted = result));
        if (!userSaveCompleted)
        {
            onCompleted?.Invoke(false);
            yield break;
        }

        if (userTask.IsCanceled || userTask.IsFaulted)
        {
            SetError(GetExceptionMessage(userTask.Exception, "자동 회원가입 저장 실패"));
            onCompleted?.Invoke(false);
            yield break;
        }

        SaveLocalBackup(userData);
        onCompleted?.Invoke(true);
#else
        SetError("Firebase가 비활성화되어 실제 회원가입을 저장할 수 없습니다. Android 빌드에서 FIREBASE_ENABLED 상태로 실행해 주세요.");
        onCompleted?.Invoke(false);
        yield return null;
#endif
    }

    private UserCloudData CreateGoogleUserCloudData(string playerId, string email, string displayName)
    {
        var userData = UserCloudData.CreateDefault();
        userData.uid = _uid;
        userData.playerId = playerId;
        userData.email = email;
        userData.displayName = displayName;
        userData.provider = "google";
        return userData;
    }

#if FIREBASE_ENABLED
    private Dictionary<string, object> CreateGoogleUserDictionary(UserCloudData userData)
    {
        // 추가: 조규민 - Google 회원가입 저장도 일반 저장과 같은 Dictionary 변환 경로를 사용해 필드 누락과 직렬화 오류를 방지한다.
        var userDictionary = CreateUserCloudDataDictionary(userData);
        userDictionary["createdAt"] = FieldValue.ServerTimestamp;
        userDictionary["lastLoginAt"] = FieldValue.ServerTimestamp;
        return userDictionary;
    }

    // 추가: 조규민 - UserCloudData를 Firestore가 안정적으로 저장할 수 있는 기본 타입 Dictionary로 변환한다.
    private Dictionary<string, object> CreateUserCloudDataDictionary(UserCloudData userData)
    {
        return new Dictionary<string, object>
        {
            { "uid", userData.uid ?? _uid },
            { "playerId", userData.playerId ?? string.Empty },
            { "email", userData.email ?? string.Empty },
            { "displayName", userData.displayName ?? string.Empty },
            { "provider", userData.provider ?? string.Empty },
            { "characterLevel", userData.characterLevel },
            { "currentChapter", userData.currentChapter },
            { "currentStage", userData.currentStage },
            { "unlockedStages", userData.unlockedStages ?? new List<int>() },
            { "gold", userData.gold },
            { "parchment", userData.parchment },
            { "hpBonus", userData.hpBonus },
            { "attackBonus", userData.attackBonus },
            { "shieldBonus", userData.shieldBonus },
            { "achievements", CreateAchievementDictionaries(userData.achievements) },
            { "enchantBookOwned", userData.enchantBookOwned ?? new List<int>() },
            { "language", userData.language },
            { "sfxVolume", userData.sfxVolume },
            { "bgmVolume", userData.bgmVolume },
            { "createdAt", userData.createdAt ?? DateTime.UtcNow.ToString("o") },
            { "lastLoginAt", userData.lastLoginAt ?? DateTime.UtcNow.ToString("o") }
        };
    }

    // 추가: 조규민 - 업적 저장 항목은 사용자 정의 클래스라 Firestore 기본 타입 Dictionary 목록으로 변환한다.
    private List<Dictionary<string, object>> CreateAchievementDictionaries(List<AchievementSaveEntry> achievements)
    {
        var achievementDictionaries = new List<Dictionary<string, object>>();
        if (achievements == null)
        {
            return achievementDictionaries;
        }

        foreach (var achievement in achievements)
        {
            if (achievement == null)
            {
                continue;
            }

            achievementDictionaries.Add(new Dictionary<string, object>
            {
                { "achievementId", achievement.achievementId },
                { "unlocked", achievement.unlocked },
                { "progress", achievement.progress }
            });
        }

        return achievementDictionaries;
    }

#endif

    private void SetError(string message)
    {
        LastError = message;
        OnError?.Invoke(message);
    }

#if FIREBASE_ENABLED
    private UserCloudData ConvertSnapshotToUserCloudData(DocumentSnapshot snapshot)
    {
        var data = UserCloudData.CreateDefault();

        if (snapshot.TryGetValue("uid", out string uid))
            data.uid = uid;

        if (snapshot.TryGetValue("playerId", out string playerId))
            data.playerId = playerId;

        if (snapshot.TryGetValue("email", out string email))
            data.email = email;

        if (snapshot.TryGetValue("displayName", out string displayName))
            data.displayName = displayName;

        if (snapshot.TryGetValue("provider", out string provider))
            data.provider = provider;

        if (snapshot.TryGetValue("characterLevel", out int characterLevel))
            data.characterLevel = characterLevel;

        if (snapshot.TryGetValue("currentChapter", out int currentChapter))
            data.currentChapter = currentChapter;

        if (snapshot.TryGetValue("currentStage", out int currentStage))
            data.currentStage = currentStage;

        if (snapshot.TryGetValue("gold", out int gold))
            data.gold = gold;

        if (snapshot.TryGetValue("parchment", out int parchment))
            data.parchment = parchment;

        if (snapshot.TryGetValue("hpBonus", out int hpBonus))
            data.hpBonus = hpBonus;

        if (snapshot.TryGetValue("attackBonus", out int attackBonus))
            data.attackBonus = attackBonus;

        if (snapshot.TryGetValue("shieldBonus", out int shieldBonus))
            data.shieldBonus = shieldBonus;

        if (snapshot.TryGetValue("language", out string language))
            data.language = language;

        if (snapshot.TryGetValue("sfxVolume", out double sfxVolume))
            data.sfxVolume = (float)sfxVolume;

        if (snapshot.TryGetValue("bgmVolume", out double bgmVolume))
            data.bgmVolume = (float)bgmVolume;

        // 추가: 조규민 - Firestore 숫자 목록 타입 차이를 흡수해 해금 스테이지를 복원한다.
        if (snapshot.TryGetValue("unlockedStages", out object unlockedStages))
            data.unlockedStages = ConvertIntList(unlockedStages);

        // 추가: 조규민 - Firestore에 Dictionary 목록으로 저장된 업적 데이터를 로컬 저장 구조로 복원한다.
        if (snapshot.TryGetValue("achievements", out object achievements))
            data.achievements = ConvertAchievements(achievements);

        // 추가: 조규민 - 인챈트 도감 보유 목록을 Firestore에서 복원한다.
        if (snapshot.TryGetValue("enchantBookOwned", out object enchantBookOwned))
            data.enchantBookOwned = ConvertIntList(enchantBookOwned);

        return data;
    }

    // 추가: 조규민 - Firestore Dictionary 목록을 AchievementSaveEntry 목록으로 변환한다.
    private List<AchievementSaveEntry> ConvertAchievements(object achievementObjects)
    {
        var achievements = new List<AchievementSaveEntry>();
        var enumerableAchievements = achievementObjects as IEnumerable;
        if (enumerableAchievements == null)
        {
            return achievements;
        }

        foreach (var achievementObject in enumerableAchievements)
        {
            var achievementDictionary = achievementObject as IDictionary<string, object>;
            if (achievementDictionary == null)
            {
                continue;
            }

            achievements.Add(new AchievementSaveEntry
            {
                achievementId = GetIntValue(achievementDictionary, "achievementId"),
                unlocked = GetBoolValue(achievementDictionary, "unlocked"),
                progress = GetIntValue(achievementDictionary, "progress")
            });
        }

        return achievements;
    }

    // 추가: 조규민 - Firestore가 int 값을 long/double로 돌려주는 경우까지 포함해 int 목록으로 변환한다.
    private List<int> ConvertIntList(object listObject)
    {
        var intList = new List<int>();
        var enumerableList = listObject as IEnumerable;
        if (enumerableList == null)
        {
            return intList;
        }

        foreach (var value in enumerableList)
        {
            intList.Add(ConvertNumberToInt(value));
        }

        return intList;
    }

    // 추가: 조규민 - Firestore 숫자 타입 차이를 흡수해 int 값으로 변환한다.
    private int GetIntValue(IDictionary<string, object> dictionary, string key)
    {
        if (!dictionary.TryGetValue(key, out var value))
        {
            return 0;
        }

        return ConvertNumberToInt(value);
    }

    // 추가: 조규민 - Firestore 숫자 object를 int로 변환한다.
    private int ConvertNumberToInt(object value)
    {
        if (value is int intValue) return intValue;
        if (value is long longValue) return (int)longValue;
        if (value is double doubleValue) return (int)doubleValue;
        return 0;
    }

    // 추가: 조규민 - Firestore Dictionary에서 bool 값을 안전하게 읽는다.
    private bool GetBoolValue(IDictionary<string, object> dictionary, string key)
    {
        if (!dictionary.TryGetValue(key, out var value))
        {
            return false;
        }

        return value is bool boolValue && boolValue;
    }

    private IEnumerator WaitForFirestoreTask(Task task, string timeoutMessage, Action<bool> onCompleted)
    {
        float elapsedTime = 0f;
        while (!task.IsCompleted && elapsedTime < FIRESTORE_TIMEOUT_SECONDS)
        {
            elapsedTime += Time.unscaledDeltaTime;
            yield return null;
        }

        if (!task.IsCompleted)
        {
            SetError(timeoutMessage);
            onCompleted?.Invoke(false);
            yield break;
        }

        onCompleted?.Invoke(true);
    }
#endif

    public void SaveLocalBackup(UserCloudData data)   // 단계④: GameManager가 오프라인 즉시저장에 쓰도록 공개
    {
        // 추가: 조규민 - 로컬 백업 실패가 로그인 흐름 전체를 중단하지 않도록 예외를 이벤트로 전달한다.
        try
        {
            File.WriteAllText(GetBackupPath(), JsonUtility.ToJson(data, true));
        }
        catch (Exception exception)
        {
            OnError?.Invoke(exception.Message);
        }
    }

    private UserCloudData LoadLocalBackup()
    {
        string path = GetBackupPath();
        if (!File.Exists(path)) return null;

        try
        {
            return JsonUtility.FromJson<UserCloudData>(File.ReadAllText(path));
        }
        catch (Exception exception)
        {
            OnError?.Invoke(exception.Message);
            return null;
        }
    }

    public bool HasLocalBackup() => File.Exists(GetBackupPath());

    private string GetBackupPath()
    {
        if (string.IsNullOrWhiteSpace(_uid))
        {
            return Path.Combine(Application.persistentDataPath, LOCAL_BACKUP_FILE);
        }

        return Path.Combine(Application.persistentDataPath, "cloud_backup_" + SanitizeFileName(_uid) + ".json");
    }

    private string SanitizeFileName(string value)
    {
        var invalidCharacters = Path.GetInvalidFileNameChars();
        foreach (char invalidCharacter in invalidCharacters)
        {
            value = value.Replace(invalidCharacter, '_');
        }

        return value;
    }

    private bool HasLocalGoogleProfileForCurrentUser()
    {
        var localData = LoadLocalBackup();
        if (localData == null)
        {
            return false;
        }

        if (!string.Equals(localData.uid, _uid, StringComparison.Ordinal))
        {
            return false;
        }

        if (!string.Equals(localData.provider, "google", StringComparison.OrdinalIgnoreCase))
        {
            return false;
        }

        return !string.IsNullOrWhiteSpace(localData.playerId);
    }

    public IEnumerator SyncLocalToCloud()
    {
        if (!HasLocalBackup()) yield break;
        var localData = LoadLocalBackup();
        if (localData == null) yield break;
        yield return StartCoroutine(SaveCoroutine(localData));
    }

    /// <summary>Firestore의 users/{uid} 문서를 삭제한다.</summary>
    public IEnumerator DeleteUserDataCoroutine(System.Action<bool> onResult)
    {
        if (string.IsNullOrEmpty(_uid))
        {
            Debug.LogWarning("[Firestore] UID 없음. 데이터 삭제 불가.");
            onResult?.Invoke(false);
            yield break;
        }

#if FIREBASE_ENABLED
        if (_db == null)
        {
            onResult?.Invoke(false);
            yield break;
        }

        bool completed = false;
        bool succeeded = false;

        _db.Collection("users").Document(_uid).DeleteAsync().ContinueWith(task =>
        {
            succeeded = !task.IsFaulted && !task.IsCanceled;
            if (task.IsFaulted)
                Debug.LogWarning("[Firestore] 유저 데이터 삭제 실패: " + task.Exception?.Message);
            completed = true;
        });

        yield return new WaitUntil(() => completed);
        onResult?.Invoke(succeeded);
#else
        onResult?.Invoke(true);
        yield break;
#endif
    }

    // 추가: 조규민 - Firebase/파일 예외 메시지를 UI와 로그에서 읽기 쉽게 정리한다.
    private string GetExceptionMessage(Exception exception, string fallbackMessage)
    {
        if (exception == null)
        {
            return fallbackMessage;
        }

        if (exception.InnerException != null)
        {
            return exception.InnerException.Message;
        }

        return exception.Message;
    }
}
