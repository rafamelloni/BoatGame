using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ObjectPool<T>
{
    Func<T> _Factory;
    Action<T> _TurnOn, _TurnOff;
    List<T> _stockAviable = new();
    public ObjectPool(Func<T> Factory, Action<T> TurnOn, Action<T> TurnOff, int inicialstock)
    {
        _Factory = Factory;
        _TurnOn = TurnOn;
        _TurnOff = TurnOff;

        for (int i = 0; i < inicialstock; i++)
        {
            var x = _Factory();

            TurnOff(x);
            _stockAviable.Add(x);
        }
    }

    public T Get()
    {
        T x;

        if (_stockAviable.Count > 0)
        {
            x = _stockAviable[0];
            _stockAviable.RemoveAt(0);
        }
        else
        {
            x = _Factory();
        }

        _TurnOn(x);
        return x;
    }
    public void Return(T value)
    {

        _TurnOff(value);
        _stockAviable.Add(value);

    }
}
