// 담당자 : 정승우
// 설명   : Firestore 저장/로드 서비스
// 2차 수정자 : 조규민
// 수정 내용 : FirebaseFirestore 필드 복구, UID/DB 방어, 로컬 백업 실패 방어 추가

#if FIREBASE_ENABLED
using Firebase.Firestore;
#endif
using System;
using System.Collections;
using System.IO;
using UnityEngine;

public class FirestoreService : MonoBehaviour
{
    public event Action<UserCloudData> OnDataLoaded;
    public event Action OnSaveComplete;
    public event Action<string> OnError;

    private string _uid;
    private const string LOCAL_BACKUP_FILE = "cloud_backup.json";

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
        if (data == null)
        {
            OnError?.Invoke("저장할 유저 데이터가 없습니다.");
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
            OnError?.Invoke("Firestore가 초기화되지 않았습니다.");
            yield break;
        }

        var docRef = _db.Collection("users").Document(_uid);
        var task = docRef.SetAsync(data);
        yield return new WaitUntil(() => task.IsCompleted);
        if (task.IsFaulted)
        {
            SaveLocalBackup(data);
            OnError?.Invoke(GetExceptionMessage(task.Exception, "저장 실패"));
            yield break;
        }
#else
        SaveLocalBackup(data);
#endif
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
            var data = task.Result.ConvertTo<UserCloudData>();
            SaveLocalBackup(data);
            OnDataLoaded?.Invoke(data);
        }
        else
        {
            var newData = UserCloudData.CreateDefault();
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

    private void SaveLocalBackup(UserCloudData data)
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
    private string GetBackupPath() => Path.Combine(Application.persistentDataPath, LOCAL_BACKUP_FILE);

    public IEnumerator SyncLocalToCloud()
    {
        if (!HasLocalBackup()) yield break;
        var localData = LoadLocalBackup();
        if (localData == null) yield break;
        yield return StartCoroutine(SaveCoroutine(localData));
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
