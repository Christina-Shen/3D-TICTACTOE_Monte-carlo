using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class startpanel : MonoBehaviour
{
    [Header("Refs")]
    [SerializeField] private Board board;
    [SerializeField] private Button btnPVP;
    [SerializeField] private Button btnPVAI;
    [SerializeField] private TMP_Text title;

    [Header("Defaults")]
    [SerializeField] private int rows = 3;
    [SerializeField] private bool thirdDimension = true;
    [SerializeField] private int match = 3;
    [SerializeField] private int aiIterations = 3000;

    private void Awake()
    {
        btnPVP.onClick.AddListener(OnPvp);
        btnPVAI.onClick.AddListener(OnPvAi);

        if (title) title.text = "3D Tic-Tac-Toe\nChoose a mode";
    }

    private void OnPvp()
    {
        GameManager.Instance.SetMode(false, rows, thirdDimension, match, aiIterations);
        CloseAndBuild();
    }

    private void OnPvAi()
    {
        GameManager.Instance.SetMode(true, rows, thirdDimension, match, aiIterations);
        CloseAndBuild();
    }

    private void CloseAndBuild()
    {
        gameObject.SetActive(false);      // 關閉 StartPanel 自己
        if (board != null) board.BuildBoard();
    }
}
