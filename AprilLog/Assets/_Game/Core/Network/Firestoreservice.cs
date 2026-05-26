// 담당자 : 정승우
// 설명   : Firestore 저장/로드 서비스

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

    public void Initialize(string uid) { _uid = uid; }

    public IEnumerator SaveCoroutine(UserCloudData data)
    {
        if (string.IsNullOrEmpty(_uid))
        {
            SaveLocalBackup(data);
            yield break;
        }
        data.lastLoginAt = DateTime.UtcNow.ToString("o");
#if FIREBASE_ENABLED
        var docRef = Firebase.Firestore.FirebaseFirestore.DefaultInstance.Collection("users").Document(_uid);
        var task = docRef.SetAsync(data);
        yield return new WaitUntil(() => task.IsCompleted);
        if (task.IsFaulted)
        {
            SaveLocalBackup(data);
            OnError?.Invoke(task.Exception?.Message ?? "저장 실패");
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
            OnDataLoaded?.Invoke(LoadLocalBackup());
            yield break;
        }
#if FIREBASE_ENABLED
        var docRef = Firebase.Firestore.FirebaseFirestore.DefaultInstance.Collection("users").Document(_uid);
        var task = docRef.GetSnapshotAsync();
        yield return new WaitUntil(() => task.IsCompleted);
        if (task.IsFaulted)
        {
            OnError?.Invoke(task.Exception?.Message ?? "로드 실패");
            OnDataLoaded?.Invoke(LoadLocalBackup());
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
        File.WriteAllText(GetBackupPath(), JsonUtility.ToJson(data, true));
    }

    private UserCloudData LoadLocalBackup()
    {
        string path = GetBackupPath();
        if (!File.Exists(path)) return null;
        return JsonUtility.FromJson<UserCloudData>(File.ReadAllText(path));
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
}