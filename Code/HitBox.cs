using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class HitBox : MonoBehaviour {
    [SerializeField] private MeshRenderer _renderer;

    [SerializeField] private GameObject _x;
    [SerializeField] private GameObject _o;

    private int _type = -1;
    public int Type => _type;

    private bool _markerPlaced = false;

    private void Start() {
        _renderer.enabled = false;
    }




    public bool MarkerPlaced => _markerPlaced;

    public void PlaceBySystem(int type)   // 玩家或 AI 都走這個
    {
        if (GameManager.Instance.GameEnd || _markerPlaced) return;

        _renderer.enabled = false;
        _markerPlaced = true;

        _type = type; // 0=X, 1=O
        var markerToSpawn = _type == 0 ? _x : _o;
        Instantiate(markerToSpawn, transform);
    }

    private void OnMouseOver() {
        if (GameManager.Instance.BlockInput || GameManager.Instance.GameEnd || _markerPlaced) return;
        _renderer.enabled = true;
    }

    private void OnMouseExit() {
        _renderer.enabled = false;
    }

    private void OnMouseUpAsButton()
    {
        // 統一交給 GameManager 判斷是否能點、該下誰
        GameManager.Instance.OnHitBoxClicked(this);
    }

    // public char GetPiece()
    // {
    //     if (_x != null && _x.activeSelf)
    //         return 'x';
    //     if (_o != null && _o.activeSelf)
    //         return 'o';
    //     return '.';
    // }
    public char GetPiece()
    {
        return _type == 0 ? 'x' : _type == 1 ? 'o' : '.';
    }
}