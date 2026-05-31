using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
public class TreeNode
{
    public GameState State { get; set; }
    public bool IsTerminal { get; set; }
    public bool IsFullyExpanded { get; set; }
    public TreeNode Parent { get; set; }
    public int Visits { get; set; }
    public float Score { get; set; }
    public Dictionary<string, TreeNode> Children { get; set; }

    public TreeNode(GameState state, TreeNode parent, bool isTerminal)
    {
        State = state;
        Parent = parent;
        IsTerminal = isTerminal;
        IsFullyExpanded = isTerminal;
        Visits = 0;
        Score = 0;
        Children = new Dictionary<string, TreeNode>();
    }
}

public class MCTS_3D
{

    private Func<GameState, bool> _isWinFunc;
    private Func<GameState, bool> _isDrawFunc;
    private Func<GameState, List<GameState>> _generateStatesFunc;
    
    private TreeNode _root;
    private System.Random _random;
    public MCTS_3D(
        Func<GameState, bool> isWinFunc,
        Func<GameState, bool> isDrawFunc,
        Func<GameState, List<GameState>> generateStatesFunc)
    {
        _isWinFunc = isWinFunc;
        _isDrawFunc = isDrawFunc;
        _generateStatesFunc = generateStatesFunc;
        _random = new System.Random();
    }

    public (int depth, int row, int col)? Search(GameState initialState, int iterations = 10000, bool verbose = false)
    {
        // 判斷初始狀態是否為終止狀態
        bool isTerminal = _isWinFunc(initialState) || _isDrawFunc(initialState);
        Debug.Log($"is terminal {isTerminal}");
        _root = new TreeNode(initialState, null, isTerminal);

        for (int iteration = 0; iteration < iterations; iteration++)
        {
            TreeNode node = Select(_root);
            float score = Rollout(node.State);
            Backpropagate(node, score);

            if (verbose && iteration % 1000 == 0)
            {
                Debug.Log($"Iteration {iteration}: Root visits={_root.Visits}, children={_root.Children.Count}");
            }
        }

        TreeNode bestNode = GetBestMove(_root, 0);

        if (bestNode == null)
            return null;

        return GetMoveCoords(initialState, bestNode.State);
    }

    private (int depth, int row, int col)? GetMoveCoords(GameState oldState, GameState newState)
    {
        for (int d = 0; d < 3; d++)
        {
            for (int r = 0; r < 3; r++)
            {
                for (int c = 0; c < 3; c++)
                {
                    if (oldState.Position[(d, r, c)] != newState.Position[(d, r, c)])
                    {
                        return (d, r, c);
                    }
                }
            }
        }
        return null;
    }


    private TreeNode Select(TreeNode node)
    {
        while (!node.IsTerminal)
        {
            if (node.IsFullyExpanded)
            {
                node = GetBestMove(node, Mathf.Sqrt(2));
            }
            else
            {
                return Expand(node);
            }
        }
        return node;
    }


    private TreeNode Expand(TreeNode node)
    {
        List<GameState> states = _generateStatesFunc(node.State);
        
        if (states.Count == 0)
        {
            node.IsFullyExpanded = true;
            return node;
        }

        foreach (var state in states)
        {
            string stateId = GetStateId(state);
            
            if (!node.Children.ContainsKey(stateId))
            {
                bool isTerminal = _isWinFunc(state) || _isDrawFunc(state);
                TreeNode newNode = new TreeNode(state, node, isTerminal);
                node.Children[stateId] = newNode;

                if (node.Children.Count == states.Count)
                {
                    node.IsFullyExpanded = true;
                }

                return newNode;
            }
        }

        node.IsFullyExpanded = true;
        return node;
    }


    private float Rollout(GameState state)
    {
        GameState currentState = state.Copy();

        while (!_isWinFunc(currentState))
        {
            List<GameState> nextStates = _generateStatesFunc(currentState);
            
            if (nextStates.Count == 0)
                return 0;

            currentState = nextStates[_random.Next(nextStates.Count)];
        }

        // 返回分數（從 'x' 的角度）
        if (currentState.Player2 == 'x')
            return 10;
        else if (currentState.Player2 == 'o')
            return -10;
        
        return 0;
    }


    private void Backpropagate(TreeNode node, float score)
    {
        while (node != null)
        {
            node.Visits++;
            node.Score += score;
            node = node.Parent;
        }
    }


    private TreeNode GetBestMove(TreeNode node, float explorationConstant)
    {
        if (node.Children.Count == 0)
            return null;

        // 優先選擇未訪問的節點
        var unvisited = node.Children.Values.Where(ch => ch.Visits == 0).ToList();
        if (unvisited.Count > 0)
        {
            return unvisited[_random.Next(unvisited.Count)];
        }

        TreeNode bestChild = null;
        float bestValue = float.NegativeInfinity;

        int totalVisits = node.Children.Values.Sum(ch => ch.Visits);
        float sign = node.State.Player1 == 'x' ? 1 : -10;

        foreach (var child in node.Children.Values)
        {
            if (child.Visits == 0)
                continue;

            float avgReward = child.Score / child.Visits;
            float exploration = explorationConstant * Mathf.Sqrt(Mathf.Log(totalVisits) / child.Visits);

            float ucbValue = sign * avgReward + exploration;

            if (ucbValue > bestValue)
            {
                bestValue = ucbValue;
                bestChild = child;
            }
        }

        return bestChild;
    }
    private string GetStateId(GameState state)
    {
        // 簡單序列化：將 3x3x3 棋盤轉成字串
        var chars = new char[27];
        int idx = 0;
        
        for (int d = 0; d < 3; d++)
        {
            for (int r = 0; r < 3; r++)
            {
                for (int c = 0; c < 3; c++)
                {
                    chars[idx++] = state.Position[(d, r, c)];
                }
            }
        }
        
        return new string(chars);
    }
}