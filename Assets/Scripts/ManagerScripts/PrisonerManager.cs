using System;
using System.Collections.Generic;
using UnityEngine;

public class PrisonerManager : MonoBehaviour
{
    [Header("Prisoner Settings")]
    [SerializeField] private PrisonerData[] prisonerData;
    [SerializeField] private PrisonerData[] badPrisonerData;

    public  Prisoner[]  Prisoners { get; private set; }

    public  Prisoner[]  BadPrisoners { get; private set; }

    private void Start()
    {
        InitPrisoners();
    }

    private void InitPrisoners()
    {
        Prisoners = new Prisoner[prisonerData.Length];
        for (int i = 0; i < prisonerData.Length; i++)
        {
            Prisoners[i] = new Prisoner(prisonerData[i]);
        }
        BadPrisoners = new Prisoner[badPrisonerData.Length];
        for (int i = 0; i < badPrisonerData.Length; i++)
        {
            BadPrisoners[i] = new Prisoner(badPrisonerData[i]);
        }
    }
    
}
