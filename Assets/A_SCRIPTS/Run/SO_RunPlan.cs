using System;
using System.Collections.Generic;
using UnityEngine;

public enum RunEventType { Wave, Shrine, Tower }

[Serializable]
public class RunEvent
{
    [Tooltip("Segundo del timer. No avanza durante los bosses.")]
    public float time;
    public RunEventType type;
    [Tooltip("Wave: cantidad de grupos. Tower: cantidad de torres. Shrine: se ignora.")]
    [Min(1)] public int amount = 1;

    public RunEvent(float time, RunEventType type, int amount = 1)
    {
        this.time = time;
        this.type = type;
        this.amount = amount;
    }
}

[CreateAssetMenu(fileName = "SO_RunPlan", menuName = "Run/Plan de la run")]
public class SO_RunPlan : ScriptableObject
{
    public List<RunEvent> events = new List<RunEvent>
    {
        // Tramo 1
        new RunEvent(30f, RunEventType.Tower),
        new RunEvent(60f, RunEventType.Shrine),
        new RunEvent(75f, RunEventType.Tower),
        new RunEvent(120f, RunEventType.Wave, 10),
        new RunEvent(125f, RunEventType.Tower),

        // Tramo 2
        new RunEvent(195f, RunEventType.Tower),
        new RunEvent(240f, RunEventType.Shrine),
        new RunEvent(245f, RunEventType.Tower),
        new RunEvent(290f, RunEventType.Tower),
        new RunEvent(300f, RunEventType.Wave, 20),
        new RunEvent(330f, RunEventType.Tower),

        // Tramo 3
        new RunEvent(375f, RunEventType.Tower),
        new RunEvent(420f, RunEventType.Shrine),
        new RunEvent(425f, RunEventType.Tower),
        new RunEvent(465f, RunEventType.Tower),
        new RunEvent(480f, RunEventType.Wave, 30),
        new RunEvent(510f, RunEventType.Tower)
    };
}
