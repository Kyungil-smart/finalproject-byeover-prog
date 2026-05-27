using System;
using UnityEngine;

public abstract class BaseMonster : MonoBehaviour, IPoolable
{
    // ---------- Data Field ----------
    public abstract int CharacterID { get; }

    

    // ---------- IPoolable ----------
    public virtual void OnSpawn()
    {
        
    }

    public virtual void OnDespawn()
    {
        
    }
    
    // ---------- 이벤트 함수 ----------
    public virtual void Awake()
    {
        
    }

    public virtual void Start()
    {
        
    }

    public virtual void Update()
    {
        
    }
}
