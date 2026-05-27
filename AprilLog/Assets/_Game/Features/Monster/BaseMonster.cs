using System;
using UnityEngine;

public abstract class BaseMonster : MonoBehaviour, IPoolable
{
    // ---------- Data Source ----------
    public abstract int CharacterID { get; }
    public abstract CommonStatusData BaseStatusData { get; }
    public abstract MonsterStatusData BaseMonsterData { get; }
    
    // ---------- Private Data Field ----------
    public abstract int CurrentHp { get; protected set; }
    public abstract int MaxHp { get; protected set; }
    public abstract int Attack { get; protected set; }
    public abstract int Defense { get; protected set; }
    
    // ---------- Const Data Field ----------
    protected const string Rate = "Rate";
    protected const string Add = "Add";
    protected const string None = "None";

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

    public virtual void FixedUpdate()
    {
        
    }

    public virtual void Update()
    {
        
    }
    
    // ---------- IPoolable ----------
    public virtual void Move()
    {
        //transform.position = Vector2.MoveTowards
    }
}
