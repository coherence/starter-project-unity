﻿using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;

public class PlayerBehaviour : MonoBehaviour
{
    public bool LocalPlayer = false;

    public string PlayerName = "Bot";
    public enum StateType
    {
        Default = 0,
        Red,
        Yellow,
        Green,
        Blue
    }

    void OnGUI()
    {
        var p = Camera.main.WorldToScreenPoint(transform.position);
        var centeredStyle = new GUIStyle(GUI.skin.label);
        centeredStyle.alignment = TextAnchor.MiddleCenter;
        centeredStyle.normal.textColor = Color.black;
        GUI.Label(new Rect(p.x - 100, Screen.height-p.y, 200, 50), PlayerName, centeredStyle);
    }

    public float movementSpeed = 3f;
    
    private StateType state = StateType.Default;

    public int State
    {
        get
        {
            return (int) state;
        }
        set
        {
            state = (StateType) value;
        }
    }

    private Material material;

    public void Awake()
    {
        var renderer = GetComponent<Renderer>();
        material = renderer.material;
        RenderState();
    }
    
    public void Update()
    {
        if(LocalPlayer)ProcessInput();
        RenderState();
    }

    private void ProcessInput()
    {
        const float threshold = 0.001f;

        float dx = UnityEngine.Input.GetAxis("Horizontal");
        float dy = UnityEngine.Input.GetAxis("Vertical");

        if (Mathf.Abs(dx) > threshold)
        {
            transform.position += Vector3.right * (movementSpeed * dx * Time.deltaTime);
        }

        if (Mathf.Abs(dy) > threshold)
        {
            transform.position += Vector3.forward * (movementSpeed * dy * Time.deltaTime);
        }

        if (UnityEngine.Input.GetKeyDown(KeyCode.Space))
        {
            CycleState();
        }
    }

    private void CycleState()
    {
        var values = Enum.GetValues(typeof(StateType));
        var maxValue = (int) values.GetValue(values.Length - 1);
        int cst = (int) state;
        cst += 1;
        if (cst > maxValue) cst = 0;
        state = (StateType) cst;
    }

    private void RenderState()
    {
        switch (state)
        {
            case StateType.Default:
                material.color = Color.white;
                break;
            case StateType.Red:
                material.color = Color.red;
                break;
            case StateType.Yellow:
                material.color = Color.yellow;
                break;
            case StateType.Green:
                material.color = Color.green;
                break;
            case StateType.Blue:
                material.color = Color.blue;
                break;
            default:
                throw new ArgumentOutOfRangeException();
        }
    }
}