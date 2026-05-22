// 담당자 : 정승우
// 설명   : Firestore 저장/로드 서비스 -- 오프라인 백업 포함

using System;
using System.Collections;
using System.IO;
using UnityEngine;

/// <summary>
/// Cloud Firestore에 유저 데이터를 저장/로드한다.
/// 네트워크 안 되면 로컬 JSON에 백업하고 나중에 동기화.
/// </summary>
public class FirestoreService : MonoBehaviour
{
    // ---------- 이벤트 ----------
    public event Action<UserCloudData> OnDataLoaded;
    public event Action OnSaveComplete;
    public event Action<string> OnError;

    // ---------- Private ----------
    // private FirebaseFirestore _db;
    private string _uid;

    private const string LOCAL_BACKUP_FILE = "cloud_backup.json";

    // ---------- 초기화 ----------
    public void Initialize(string uid)
    {
        _uid = uid;
#if FIREBASE_ENABLED
        _db = FirebaseFirestore.DefaultInstance;
#endif
    }

    // ---------- 저장 ----------
    public IEnumerator SaveCoroutine(UserCloudData data)
    {
        if (string.IsNullOrEmpty(_uid))
        {
            Debug.LogWarning("[Firestore] UID 없음. 로컬에만 저장.");
            SaveLocalBackup(data);
            yield break;
        }

        data.lastLoginAt = DateTime.UtcNow.ToString("o");

#if FIREBASE_ENABLED
        var docRef = _db.Collection("users").Document(_uid);
        var task = docRef.SetAsync(data);
        yield return new WaitUntil(() => task.IsCompleted);

        if (task.IsFaulted)
        {
            Debug.LogWarning("[Firestore] 클라우드 저장 실패. 로컬 백업 저장.");
            SaveLocalBackup(data);
            OnError?.Invoke(task.Exception?.Message ?? "저장 실패");
            yield break;
        }

        Debug.Log("[Firestore] 클라우드 저장 완료");
#else
        // Firebase 없으면 로컬에만
        Debug.Log("[Firestore] FIREBASE_ENABLED 꺼짐. 로컬 백업만.");
        SaveLocalBackup(data);
#endif

        OnSaveComplete?.Invoke();
    }

    // ---------- 로드 ----------
    public IEnumerator LoadCoroutine()
    {
        if (string.IsNullOrEmpty(_uid))
        {
            Debug.LogWarning("[Firestore] UID 없음. 로컬 백업에서 로드.");
            var local = LoadLocalBackup();
            OnDataLoaded?.Invoke(local);
            yield break;
        }

#if FIREBASE_ENABLED
        var docRef = _db.Collection("users").Document(_uid);
        var task = docRef.GetSnapshotAsync();
        yield return new WaitUntil(() => task.IsCompleted);

        if (task.IsFaulted)
        {
            Debug.LogWarning("[Firestore] 클라우드 로드 실패. 로컬 백업 시도.");
            var local = LoadLocalBackup();
            OnDataLoaded?.Invoke(local);
            yield break;
        }

        var snapshot = task.Result;

        if (snapshot.Exists)
        {
            var data = snapshot.ConvertTo<UserCloudData>();
            SaveLocalBackup(data);  // 로컬에도 백업
            Debug.Log("[Firestore] 클라우드 데이터 로드 완료");
            OnDataLoaded?.Invoke(data);
        }
        else
        {
            // 신규 유저
            var newData = UserCloudData.CreateDefault();
            yield return StartCoroutine(SaveCoroutine(newData));
            Debug.Log("[Firestore] 신규 유저. 기본 데이터 생성.");
            OnDataLoaded?.Invoke(newData);
        }
#else
        Debug.Log("[Firestore] FIREBASE_ENABLED 꺼짐. 로컬 백업 로드.");
        var localData = LoadLocalBackup();
        if (localData == null)
        {
            localData = UserCloudData.CreateDefault();
            SaveLocalBackup(localData);
        }
        OnDataLoaded?.Invoke(localData);
        yield return null;
#endif
    }

    // ---------- 로컬 백업 ----------
    // 네트워크 안 될 때 여기 저장하고, 복구되면 클라우드로 올림

    private void SaveLocalBackup(UserCloudData data)
    {
        string json = JsonUtility.ToJson(data, true);
        string path = GetBackupPath();
        File.WriteAllText(path, json);
    }

    private UserCloudData LoadLocalBackup()
    {
        string path = GetBackupPath();
        if (!File.Exists(path)) return null;

        string json = File.ReadAllText(path);
        return JsonUtility.FromJson<UserCloudData>(json);
    }

    public bool HasLocalBackup()
    {
        return File.Exists(GetBackupPath());
    }

    private string GetBackupPath()
    {
        return Path.Combine(Application.persistentDataPath, LOCAL_BACKUP_FILE);
    }

    // ---------- 동기화 ----------
    // 오프라인에서 온라인으로 복구됐을 때 호출
    public IEnumerator SyncLocalToCloud()
    {
        if (!HasLocalBackup()) yield break;

        var localData = LoadLocalBackup();
        if (localData == null) yield break;

        yield return StartCoroutine(SaveCoroutine(localData));
        Debug.Log("[Firestore] 로컬 -> 클라우드 동기화 완료");
    }
}