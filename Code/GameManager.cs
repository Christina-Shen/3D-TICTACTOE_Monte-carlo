using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;


public class GameManager : MonoBehaviour {
    private static GameManager _instance;

    public static GameManager Instance =>
        _instance ? _instance : new GameObject("Game Manager").AddComponent<GameManager>();

    private int _rows;
    private int _turn;
    private int _match = 3;
    private bool _gameEnd = false;
    //private bool _devMode = true;
    private bool _thirdDimension = false;
    private Board _board;
    private Dictionary<string, HitBox> _fields = new Dictionary<string, HitBox>();
    private List<HitBox> _matchedPattern = new List<HitBox>();

    public int Turn => _turn % 2;
    public int Rows => _rows;
    public int Match => _match;
    public bool GameEnd => _gameEnd;
    //public bool DevMode => _devMode;
    public Board Board => _board;
    public List<HitBox> Pattern => _matchedPattern;
    private bool _pveMode = true; 
    private int _maxMoves => _thirdDimension ? _rows * _rows * _rows : _rows * _rows;
    private MCTS_3D _mcts;
        // P vs AI
    private int  _aiIterations = 3000; // MCTS 次數
 

    public bool BlockInput { get; private set; } = false; 
   // private Mcts3D _mcts;

    public event Action<bool, int> OnGameEnd;


    void Awake() {
        _instance = this;
        DontDestroyOnLoad(gameObject);
        //_devMode = false;
    }

    public void Set(int rows, bool thirdDimension, int match = 3) {
        _rows = rows;
        _thirdDimension = thirdDimension;
        _match = match;
    }

    public void AddHitBox(HitBox hitBox, int d, int r, int c)
    {
        _fields[$"{d}{r}{c}"] = hitBox; // ❶ 統一 (d,r,c)
    }

    public void Clear() {
        _fields.Clear();
        _matchedPattern?.Clear();
        _gameEnd = false;
        _turn = 0;
        OnGameEnd?.Invoke(_gameEnd, -1);
    }
    public void SetMode(bool pveMode, int rows = 3, bool thirdDimension = true, int match = 3, int aiIterations = 3000)
    {
        _pveMode = pveMode;
        _aiIterations = aiIterations;
        Set(rows, thirdDimension, match);

        // 重置狀態（開始新局）
        Clear();
    }

    public void MoveMade() {
        _turn++;

        _matchedPattern = PatternFinder.CheckWin(_fields);

        if (_matchedPattern != null && _matchedPattern.Count > 0) {
            _gameEnd = true;
            OnGameEnd?.Invoke(_gameEnd, _matchedPattern[0].Type);
        }
        else if (_turn >= _maxMoves) {
            _gameEnd = true;
            OnGameEnd?.Invoke(_gameEnd, -1);
        }
    }


    // public bool ToggleDevMode() {
    //     _devMode = !_devMode;
    //     return _devMode;
    // }

    public void SetBoard(Board board) {
        _board = board;
    }
    
 
    // MCTS choose /mademove =================================

    private bool _aiThinking = false;

    void Start() {
        //_mcts = new Mcts3D();               // 只建一次
        // 如果你的 Mcts3D 沒有 Reset() 就把下一行刪掉
        // _mcts.Reset(BuildStateForMcts()); 

        _mcts = new MCTS_3D(
                isWinFunc: GameRules.IsWinAny,   // ❹ 改用「任一方」的勝利判斷（見下）
                isDrawFunc: GameRules.IsDraw,
                generateStatesFunc: GameRules.GenerateStates
            );
    }

    public void MaybeAIMove()
    {
        if (!_pveMode || GameEnd) return;
        if (Turn != 1) return;              // AI 是 O，只在 O 回合
        if (_aiThinking) return;            // 防止重入
        StartCoroutine(AiMoveRoutine());
    }

    private IEnumerator AiMoveRoutine()
    {
        _aiThinking = true;
        BlockInput = true;
        yield return null;

        var currentState = ToGameState();
        //Debug_LogCurrentState();
        var move = _mcts.Search(currentState, iterations: _aiIterations, verbose: false); // ❺ 用 _mcts
        // var (d, r, c) = move.Value;
        // Debug.Log($"[AI Move] depth={d}, row={r}, col={c}");
        if (move.HasValue && !GameEnd)
        {
            var (d, r, c) = move.Value;
            Debug.Log($"[AI Move] depth={d}, row={r}, col={c}");
            string key = $"{d}{r}{c}"; // ❻ 與 AddHitBox / ToGameState 一致
            
            if (_fields.TryGetValue(key, out var hb) && hb != null && !hb.MarkerPlaced)
            {
                hb.PlaceBySystem(1); // O
                MoveMade();
            }
        }
        else
        {
            Debug.LogWarning("MCTS 沒有提出步驟（可能已終局或搜尋失敗）。");
        }

        BlockInput = false;
        _aiThinking = false;
    }
    // hitbox using:
    public void OnHitBoxClicked(HitBox hb)
    {
        // 空格才可下；終局或 AI 正在思考時就不能下
        if (GameEnd || BlockInput || hb.Type != -1) return;

        // 依回合決定是 X(0) 還是 O(1)
        int who = (Turn == 0) ? 0 : 1;
        hb.PlaceBySystem(who);
        MoveMade();

        // 只有 P v AI 才讓 AI 動
        if (_pveMode)
            MaybeAIMove();
    }


        // ========GameState===============to GameState=================================

    public GameState ToGameState()
    {
        var position = new Dictionary<(int, int, int), char>();

        for (int d = 0; d < 3; d++)
        for (int r = 0; r < 3; r++)
        for (int c = 0; c < 3; c++)
        {
            string key = $"{d}{r}{c}";
            if (_fields.TryGetValue(key, out var hitBox) && hitBox != null)
                position[(d, r, c)] = hitBox.GetPiece();   // 'x','o','.'
            else
                position[(d, r, c)] = '.';                 // ❷ 一定要補空格
        }

        // 決定輪到誰下：Turn==0 → X to move；Turn==1 → O to move
        char player1 = (Turn == 0) ? 'x' : 'o'; // 要下子的那一位
        char player2 = (Turn == 0) ? 'o' : 'x'; // 對手

        return new GameState(position, player1, player2);
    }
   



}

// 調試：把 _fields -> GameState，並把盤面漂亮地印出來
