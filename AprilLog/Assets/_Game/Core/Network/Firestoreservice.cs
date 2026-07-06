// 담당자 : 정승우
// 설명   : Firestore 저장/로드 서비스
// 2차 수정자 : 조규민
// 수정 내용 : FirebaseFirestore 필드 복구, UID/DB 방어, 로컬 백업 실패 방어 추가
// 3차 수정자 : 조규민
// 수정 내용 : UserCloudData 직접 저장 대신 Firestore Dictionary 변환 저장으로 직렬화 오류 수정

// 수정 내용 : 하우징 구매 보유 가구 ID를 Firestore 저장/로드에 포함

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

// 3차 수정자 : 조규민
// 수정 내용 : 하우징 자동재화 마지막 수령 시간 저장/로드 필드, 계정별 최초 진입 상태 저장/로드 및 기존 계정 감지 추가
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

        // 추가: 조규민 - 네트워크 저장을 기다리는 동안 앱이 종료되어도 최초 진입 상태가 유실되지 않게 먼저 로컬에 기록한다.
        SaveLocalBackup(data);

        if (string.IsNullOrEmpty(_uid))
        {
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
            MergeLocalInitialFlowState(data);
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
            { "hasInitialFlowState", userData._hasInitialFlowState },
            { "initialStoryStarted", userData._initialStoryStarted },
            { "tutorialCompleted", userData._tutorialCompleted },
            { "gold", userData.gold },
            { "parchment", userData.parchment },
            { "diamond", userData.diamond },
            { "housingAutoCurrencyLastClaimAt", userData.housingAutoCurrencyLastClaimAt ?? DateTime.UtcNow.ToString("o") },
            { "housingPlacedFurnitureIds", userData.housingPlacedFurnitureIds ?? new List<int>() },
            { "housingOwnedFurnitureIds", userData.housingOwnedFurnitureIds ?? new List<int>() },
            { "hpBonus", userData.hpBonus },
            { "attackBonus", userData.attackBonus },
            { "shieldBonus", userData.shieldBonus },
            { "achievements", CreateAchievementDictionaries(userData.achievements) },
            { "enchantBookOwned", userData.enchantBookOwned ?? new List<int>() },
            // 아래 4필드가 저장/복원 양쪽에서 빠져 있어 클라우드 왕복 한 번에 아이템/스태미나/아티팩트/최초클리어 기록이
            // 전부 소멸했다(로드 직후 SaveLocalBackup이 깎인 데이터로 로컬 백업까지 덮음). 반드시 복원 코드와 쌍으로 유지할 것.
            { "firstClearRewardedStages", userData.firstClearRewardedStages ?? new List<int>() },
            { "inventory", CreateItemDictionaries(userData.inventory) },
            { "staminaData", CreateStaminaDictionaries(userData.staminaData) },
            { "myArtifacts", CreateArtifactDictionaries(userData.myArtifacts) },
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

    // 인벤토리 항목을 Firestore 기본 타입 Dictionary 목록으로 변환한다.
    private List<Dictionary<string, object>> CreateItemDictionaries(List<ItemSaveEntry> items)
    {
        var itemDictionaries = new List<Dictionary<string, object>>();
        if (items == null) return itemDictionaries;

        foreach (var item in items)
        {
            if (item == null) continue;
            itemDictionaries.Add(new Dictionary<string, object>
            {
                { "itemId", item.itemId },
                { "amount", item.amount }
            });
        }

        return itemDictionaries;
    }

    // 스태미나 항목(오프라인 회복용 타임스탬프 포함)을 Dictionary 목록으로 변환한다.
    private List<Dictionary<string, object>> CreateStaminaDictionaries(List<StaminaSaveEntry> staminaEntries)
    {
        var staminaDictionaries = new List<Dictionary<string, object>>();
        if (staminaEntries == null) return staminaDictionaries;

        foreach (var stamina in staminaEntries)
        {
            if (stamina == null) continue;
            staminaDictionaries.Add(new Dictionary<string, object>
            {
                { "staminaId", stamina.staminaId },
                { "currentAmount", stamina.currentAmount },
                { "lastUpdateTime", stamina.lastUpdateTime ?? string.Empty }
            });
        }

        return staminaDictionaries;
    }

    // 아티팩트 보유 목록을 Dictionary 목록으로 변환한다. (IsAscended는 AscensionCount 계산 프로퍼티라 저장하지 않는다)
    private List<Dictionary<string, object>> CreateArtifactDictionaries(List<ArtifactInstance> artifacts)
    {
        var artifactDictionaries = new List<Dictionary<string, object>>();
        if (artifacts == null) return artifactDictionaries;

        foreach (var artifact in artifacts)
        {
            if (artifact == null) continue;
            artifactDictionaries.Add(new Dictionary<string, object>
            {
                { "uniqueId", artifact.UniqueId },
                { "masterId", artifact.MasterId },
                { "currentLevel", artifact.CurrentLevel },
                { "currentCount", artifact.CurrentCount },
                { "isEquipped", artifact.IsEquipped },
                { "ascensionCount", artifact.AscensionCount }
            });
        }

        return artifactDictionaries;
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

        // 추가: 조규민 - 상태 필드가 없는 기존 계정을 신규 계정과 구분해 GameManager에서 한 번만 마이그레이션한다.
        if (snapshot.TryGetValue("hasInitialFlowState", out bool hasInitialFlowState))
            data._hasInitialFlowState = hasInitialFlowState;
        else
            data._hasInitialFlowState = false;

        if (snapshot.TryGetValue("initialStoryStarted", out bool initialStoryStarted))
            data._initialStoryStarted = initialStoryStarted;

        if (snapshot.TryGetValue("tutorialCompleted", out bool tutorialCompleted))
            data._tutorialCompleted = tutorialCompleted;

        if (snapshot.TryGetValue("gold", out int gold))
            data.gold = gold;

        if (snapshot.TryGetValue("parchment", out int parchment))
            data.parchment = parchment;

        if (snapshot.TryGetValue("diamond", out int diamond))
            data.diamond = diamond;

        if (snapshot.TryGetValue("housingAutoCurrencyLastClaimAt", out string housingAutoCurrencyLastClaimAt))
            data.housingAutoCurrencyLastClaimAt = housingAutoCurrencyLastClaimAt;
        else
            data.housingAutoCurrencyLastClaimAt = null;

        // 추가: 조규민 - Firestore에 저장된 하우징 배치 가구 ID 목록을 복원한다.
        if (snapshot.TryGetValue("housingPlacedFurnitureIds", out object housingPlacedFurnitureIds))
            data.housingPlacedFurnitureIds = ConvertIntList(housingPlacedFurnitureIds);

        // 추가: 조규민 - 계정에 저장된 하우징 구매 보유 가구 ID 목록을 복원한다.
        if (snapshot.TryGetValue("housingOwnedFurnitureIds", out object housingOwnedFurnitureIds))
            data.housingOwnedFurnitureIds = ConvertIntList(housingOwnedFurnitureIds);

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

        // 저장 딕셔너리와 쌍: 이 4필드 복원이 없으면 클라우드 로드 한 번에 아이템/스태미나/아티팩트/최초클리어 기록이 초기화된다.
        if (snapshot.TryGetValue("firstClearRewardedStages", out object firstClearRewardedStages))
            data.firstClearRewardedStages = ConvertIntList(firstClearRewardedStages);

        if (snapshot.TryGetValue("inventory", out object inventory))
            data.inventory = ConvertItems(inventory);

        if (snapshot.TryGetValue("staminaData", out object staminaData))
            data.staminaData = ConvertStaminaEntries(staminaData);

        if (snapshot.TryGetValue("myArtifacts", out object myArtifacts))
            data.myArtifacts = ConvertArtifacts(myArtifacts);

        // 계정 생성일 복원. 없으면 CreateDefault의 현재시각이 남아 저장 때마다 생성일이 전진한다.
        if (snapshot.TryGetValue("createdAt", out string createdAt))
            data.createdAt = createdAt;

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

    // Firestore Dictionary 목록을 ItemSaveEntry 목록으로 복원한다.
    private List<ItemSaveEntry> ConvertItems(object itemObjects)
    {
        var items = new List<ItemSaveEntry>();
        var enumerableItems = itemObjects as IEnumerable;
        if (enumerableItems == null) return items;

        foreach (var itemObject in enumerableItems)
        {
            var itemDictionary = itemObject as IDictionary<string, object>;
            if (itemDictionary == null) continue;

            items.Add(new ItemSaveEntry
            {
                itemId = GetIntValue(itemDictionary, "itemId"),
                amount = GetIntValue(itemDictionary, "amount")
            });
        }

        return items;
    }

    // Firestore Dictionary 목록을 StaminaSaveEntry 목록으로 복원한다.
    private List<StaminaSaveEntry> ConvertStaminaEntries(object staminaObjects)
    {
        var staminaEntries = new List<StaminaSaveEntry>();
        var enumerableEntries = staminaObjects as IEnumerable;
        if (enumerableEntries == null) return staminaEntries;

        foreach (var staminaObject in enumerableEntries)
        {
            var staminaDictionary = staminaObject as IDictionary<string, object>;
            if (staminaDictionary == null) continue;

            staminaEntries.Add(new StaminaSaveEntry
            {
                staminaId = GetIntValue(staminaDictionary, "staminaId"),
                currentAmount = GetIntValue(staminaDictionary, "currentAmount"),
                lastUpdateTime = GetStringValue(staminaDictionary, "lastUpdateTime")
            });
        }

        return staminaEntries;
    }

    // Firestore Dictionary 목록을 ArtifactInstance 목록으로 복원한다.
    private List<ArtifactInstance> ConvertArtifacts(object artifactObjects)
    {
        var artifacts = new List<ArtifactInstance>();
        var enumerableArtifacts = artifactObjects as IEnumerable;
        if (enumerableArtifacts == null) return artifacts;

        foreach (var artifactObject in enumerableArtifacts)
        {
            var artifactDictionary = artifactObject as IDictionary<string, object>;
            if (artifactDictionary == null) continue;

            artifacts.Add(new ArtifactInstance
            {
                UniqueId = GetIntValue(artifactDictionary, "uniqueId"),
                MasterId = GetIntValue(artifactDictionary, "masterId"),
                CurrentLevel = GetIntValue(artifactDictionary, "currentLevel"),
                CurrentCount = GetIntValue(artifactDictionary, "currentCount"),
                IsEquipped = GetBoolValue(artifactDictionary, "isEquipped"),
                AscensionCount = GetIntValue(artifactDictionary, "ascensionCount")
            });
        }

        return artifacts;
    }

    // Firestore Dictionary에서 string 값을 안전하게 읽는다.
    private string GetStringValue(IDictionary<string, object> dictionary, string key)
    {
        return dictionary.TryGetValue(key, out var value) && value is string stringValue ? stringValue : null;
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

    private void MergeLocalInitialFlowState(UserCloudData cloudData)
    {
        if (cloudData == null)
        {
            return;
        }

        UserCloudData localData = LoadLocalBackup();
        if (localData == null || !string.Equals(localData.uid, _uid, StringComparison.Ordinal))
        {
            return;
        }

        // 로컬의 완료 방향 상태만 병합해 네트워크 저장 직전 강제 종료로 최초 콘텐츠가 반복되는 것을 막는다.
        cloudData._initialStoryStarted |= localData._initialStoryStarted;
        cloudData._tutorialCompleted |= localData._tutorialCompleted;
    }

    public bool HasLocalBackup() => File.Exists(GetBackupPath());

    // 계정 삭제/초기화 시 로컬 백업(cloud_backup*.json)을 전부 지운다.
    // 게스트는 로컬 백업이 사실상 유일한 저장소라, 이걸 남기면 재로그인 시 옛 데이터가 복원된다.
    public void DeleteLocalBackup()
    {
        try
        {
            foreach (string file in Directory.GetFiles(Application.persistentDataPath, "cloud_backup*.json"))
                File.Delete(file);
        }
        catch (Exception exception)
        {
            OnError?.Invoke(exception.Message);
        }
    }

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
