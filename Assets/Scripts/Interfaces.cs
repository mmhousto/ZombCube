using System.Collections;
using System.Collections.Generic;
using UnityEngine;


public interface IDamageable<T>{

    void Damage(T damageTaken);
}

public interface IBuyable
{
    void BuyBlaster(int index);

}

public interface IUseable
{
    void Use(int index);
}



