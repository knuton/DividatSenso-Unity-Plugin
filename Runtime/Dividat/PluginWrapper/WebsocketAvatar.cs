﻿using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using SimpleJSON;
using Dividat;

using NativeWebSocket;

public class WebsocketAvatar : MonoBehaviour
{
    WebSocket _websocket;
    [Header("Network Configuration")]
    public string serverURL = "wss://rooms.dividat.com/rooms/unity-avatar/join";
    public bool automaticReconnect = true;
    [Range(0.2f, 10f)]
    public float retryEvery = 2f;

    public bool Connected {
        get { return _connected;}
    }
    private bool _connected = false;
    private float _retryTimer = 0f;

    private static Hardware.DirectionCallback _onStep;
    private static Hardware.DirectionCallback _onRelease;
    private static Hardware.PlateCallback _onSensoState;

    // Start is called before the first frame update
    void Start()
    {
      Debug.Log("Starting EGI");
      Connect();
    }

    public static void Register(Hardware.DirectionCallback onStep, Hardware.DirectionCallback onRelease, Hardware.PlateCallback onSensoState)
    {
      _onStep = onStep;
      _onRelease = onRelease;
      _onSensoState = onSensoState;
    }

    ///Note that the connection request is asynchronous. You can not expect values right away, but only after connected is true.
    public void Connect(){
        ConnectSocket();
    }
    void ConnectSocket(){
        Connect(serverURL);
    }

    async void Connect(string url){
        _websocket = new WebSocket(url);

        _websocket.OnOpen += () =>
        {
            Debug.Log("EGI WS avatar connection open!");
            _connected = true;

        };

        _websocket.OnError += (e) =>
        {
            Debug.Log("Error in EGI WS avatar: " + e);
        };

        _websocket.OnClose += (e) =>
        {
            Debug.Log("EGI WS avatar connection closed!");
            _connected = false;
            _retryTimer = 0f;
        };

        _websocket.OnMessage += (bytes) =>
        {
            // Reading a plain text message
            var message = System.Text.Encoding.UTF8.GetString(bytes);
            var json = JSON.Parse(message);

            switch (json["type"].Value)
            {
                case "Step":
                    _onStep(1);
                    break;
                case "Release":
                    _onRelease(1);
                    break;
                case "SensoState":
                    _onSensoState(0,
                        json["state"]["center"]["x"].AsFloat,
                        json["state"]["center"]["y"].AsFloat,
                        json["state"]["center"]["f"].AsFloat);
                    _onSensoState(1,
                        json["state"]["up"]["x"].AsFloat,
                        json["state"]["up"]["y"].AsFloat,
                        json["state"]["up"]["f"].AsFloat);
                    _onSensoState(2,
                        json["state"]["right"]["x"].AsFloat,
                        json["state"]["right"]["y"].AsFloat,
                        json["state"]["right"]["f"].AsFloat);
                    _onSensoState(3,
                        json["state"]["down"]["x"].AsFloat,
                        json["state"]["down"]["y"].AsFloat,
                        json["state"]["down"]["f"].AsFloat);
                    _onSensoState(4,
                        json["state"]["left"]["x"].AsFloat,
                        json["state"]["left"]["y"].AsFloat,
                        json["state"]["left"]["f"].AsFloat);
                    break;
            }

            _retryTimer = 0f;
        };
        await _websocket.Connect();
    }

    void Update()
    {
        if (_websocket != null){
            #if !UNITY_WEBGL || UNITY_EDITOR
                _websocket.DispatchMessageQueue();
            #endif

            if (automaticReconnect && !_connected){
                _retryTimer += Time.deltaTime;
                if (_retryTimer >= retryEvery){
                    _retryTimer=0f;
                    _websocket.Connect();
                }
            }
        }
    }

    async void SendWebSocketTextMessage(string message)
    {
        if (_websocket.State == WebSocketState.Open)
        {
            await _websocket.SendText(message);
        }
    }

    async void SendWebSocketBinaryMessage(byte[] binaryMessage)
    {
        if (_websocket.State == WebSocketState.Open)
        {
            // Sending bytes
            await _websocket.Send(binaryMessage);
        }
    }

    private async void OnDestroy()
    {
        await _websocket.Close();
    }
}
