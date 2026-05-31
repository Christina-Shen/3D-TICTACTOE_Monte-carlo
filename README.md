
# 3D tictactoe MCNT:

## MCTS algorithm and work flow : 
<img width="1570" height="1057" alt="image" src="https://github.com/user-attachments/assets/c53a501f-5079-436c-905b-d8c79820510f" />


https://github.com/maksimKorzh/tictactoe-mtcs/blob/master/src/tictactoe/mcts.py


https://github.com/JustinBieshaar/part-6-3d-tic-tac-toe




MCTS 演算法:

<img width="1532" height="705" alt="image" src="https://github.com/user-attachments/assets/eb04b348-1c5b-4d06-b584-0b9dc5d027a9" />


## GameManager.cs: 

* Singleton（單例模式）: 

9~10 =: 
```clike=
public static GameManager Instance {
    get {
        if (_instance != null)
            return _instance;
        else
            return new GameObject("Game Manager").AddComponent<GameManager>();
    }
}
```

* What is List<HitBox> 
    
*  public event Action<bool, int> OnGameEnd;
:::spoiler
    event Action<bool, int> is like declaring a function signature that can hold multiple functions.
Think of it as:

Action<bool, int> = a function that takes (bool, int) parameters and returns void
    
    
好處 : 
用 += 訂閱函數到事件
用 ?.Invoke() 安全地觸發所有訂閱的函數
所有函數按訂閱順序執行
    
```csharp=
OnGameEnd += UpdateUI;     // 清單：[UpdateUI]
OnGameEnd += PlaySound;   // 清單：[UpdateUI, PlaySound]  
OnGameEnd += SaveScore;   // 清單：[UpdateUI, PlaySound, SaveScore]
// 如果 OnGameEnd 不是 null，就執行 Invoke
OnGameEnd?.Invoke(true, 100);

// 等同於：
if (OnGameEnd != null)
{
    OnGameEnd.Invoke(true, 100);
}
```
:::
    
    
* this / DontDestroyOnLoad
:::spoiler
_instance = this;
_instance 是指向這個物件實例，不是 class。
    
DontDestroyOnLoad(gameObject);
這是 Unity 內建函數，不是你自定義的。
作用： 讓這個 GameObject 在切換場景時不被銷毀
:::
    
* Dictionary<TKey, TValue> 是什麼？
:::spoiler
    
* declare/ add/ claer: 
```csharp=
Dictionary<string, HitBox> _fields = new Dictionary<string, HitBox>();
    
// 新增或修改
_fields["attack1"] = new HitBox();
_fields.Add("defense", hitBoxObject);  // Key不存在才能用

// TryAdd (較安全)
_fields.TryAdd("special", hitBoxObject); // Key存在時不會覆蓋
    
    
// 直接取值 (Key不存在會拋例外)
HitBox hitBox = _fields["attack1"];

// 安全取值
if (_fields.TryGetValue("attack1", out HitBox result))
{
    // 找到了，使用 result
}

// 檢查是否存在
if (_fields.ContainsKey("attack1"))
{
    // Key 存在
}
    

// 移除特定 Key
_fields.Remove("attack1");

// 清空全部
_fields.Clear();
    
    
// 遍歷所有鍵值對
foreach (var pair in _fields)
{
    string key = pair.Key;
    HitBox value = pair.Value;
}

// 只遍歷 Key
foreach (string key in _fields.Keys)
{
    // ...
}

// 只遍歷 Value
foreach (HitBox hitBox in _fields.Values)
{
    // ...
}

int count = _fields.Count;        // 元素數量
var keys = _fields.Keys;          // 所有鍵的集合
var values = _fields.Values;      // 所有值的集合
    
```
:::
    
    
* OnGameEnd function is define in GameUI.cs, winline.cs:

    
## GameUI.cs: 

* readonly: 
:::spoiler
    readonly 的意思

在 C# 裡：

readonly 表示這個欄位 只能在宣告或建構子 (constructor/Awake/Start) 初始化，之後就不能被修改。

和 const 的差別：

const 必須在編譯期就確定值（只能是常數）。

readonly 可以在執行期由程式算出來一次，之後固定不變。
:::
* Animator.StringToHash:
    
:::spoiler
```clike 
它的作用是将一个字符串（例如动画参数的名称）转换为一个整数的哈希值（Hash ID）。 使用哈希值比直接使用字符串作为动画参数的引用更加高效且不容易出错，因为它避免了重复的字符串查找，并减少了拼写错误的风险。 
```
:::
    
* On GameEnd function : Why set state==_currentState return : 
:::spoiler
    為什麼 if (state == _currentState) return;

OnGameEnd 是被事件驅動的：GameManager.Instance.OnGameEnd += OnGameEnd;

一局內可能在多個時間點被呼叫（例如 Reset 時先發 end=false 隱藏、勝利時發 end=true 顯示，甚至外部重複發相同狀態）。

state 只有兩種：WIN_SHOW_STATE（顯示勝利畫面）或 WIN_HIDE_STATE（隱藏）。

如果当前畫面已經是該狀態（_currentState 記錄了上一次對 Animator 發過的 Trigger），再觸發一次沒有意義，還可能造成：

Animator 重複觸發同一個 Trigger，導致動畫重新播放或閃爍。

無謂的 UI 更新（重設文字、重跑動畫過場）。

所以這個判斷讓 OnGameEnd 冪等：同一狀態重複進來不再做事，界面穩定不抖動。
    
:::
    
* Animator介紹: 

:::spoiler
在 Animator Controller 裡的幾個元素

State（狀態）

    代表某段動畫，例如：Idle、Run、Jump、Show、Hide。

    這些就是方塊狀態機的「節點」。

Parameter（參數）

    代表動畫切換條件，例如：

    Trigger：按一次就觸發（常用來播一次動畫）

    Bool：開/關狀態

    Float、Int：連續數值，用在 Blend Tree 或判斷分支

Transition（轉場條件）

    從一個 State 轉到另一個 State，需要參數作為條件。
    
    
Parameters 區塊 Trigger "show" Trigger "hide"

States 區塊

    Hidden（隱藏 UI 的狀態，可能是透明、關閉文字）

    Shown（顯示勝利 UI 的狀態，播放彈出動畫）

Transitions（轉場規則）

    Hidden → Shown 條件：Trigger == "show"

    Shown → Hidden 條件：Trigger == "hide"
:::
    
    
    
# Board.cs :   
 ![image](https://hackmd.io/_uploads/B1p__J15el.png)
    
* Slider:
:::spoiler
Slider 是什麼？

所屬命名空間：UnityEngine.UI
是 Unity UI 系統（UGUI）裡的 滑桿元件。
它的主要功能是：提供一個可拖拉的數值輸入方式。
結構

Slider 是一個 C# 類別，繼承自 Selectable，本質上就是一個 Component，你需要掛在 Unity 的 UI → Slider 物件上。

| 成員               | 型別                  | 意義          |
| ---------------- | ------------------- | ----------- |
| `value`          | `float`             | 滑桿目前的數值     |
| `minValue`       | `float`             | 滑桿最小值       |
| `maxValue`       | `float`             | 滑桿最大值       |
| `wholeNumbers`   | `bool`              | 是否只允許整數     |
| `onValueChanged` | `UnityEvent<float>` | 當數值改變時觸發的事件 |

  在 Unity（其實是 C# event 系統 + UnityEvent 包裝）裡，AddListener 的意思是：把一個函式（方法）註冊到事件清單裡，當事件發生時，Unity 會自動呼叫你加進去的函式。

🔹 以 Slider.onValueChanged 為例

onValueChanged 是 Slider 提供的一個 UnityEvent<float>`

型別是 UnityEvent，特殊的事件物件，能儲存一組「要呼叫的函式清單」。

泛型 <float> 代表這個事件會傳一個 float 參數（滑桿的新值）。

🔹 AddListener

AddListener(函式) → 把這個函式加入事件清單。

之後，只要滑桿數值改變，onValueChanged 事件就會觸發，所有加過的 listener 都會被呼叫。  
    
:::

* Generate Function:
:::spoiler
1️⃣ var parent = Instantiate(_planePrefab, transform);

建立一個新的物件（複製 _planePrefab 預製件）。

transform = 把新物件掛在這個 script 所屬物件底下（成為子物件）。

回傳的 parent 就是剛建立的那個 GameObject。
    
    
    
    
:::
    
# WinLine.cs: 
    
這個函式在遊戲結束時：

如果平手 → 不畫線

如果有贏家 → 取得勝利的 HitBox 列表

用 LineRenderer 在場景裡畫一條線，把這些格子連起來（井字棋的「三連線」高亮效果）
 
* What is LineRenderer: 
:::spoiler
  LineRenderer 是什麼？

Component（元件），掛在 GameObject 上。

用來 畫一條或多條連續的線段。

每個線段由一系列 點 (positions) 組成，Unity 會自動在這些點之間畫直線。

常見用途：

畫雷射光束、路徑、連線效果、範圍提示…

在你的專案裡 → 畫出井字棋的「勝利線」。

* positionCount

型別：int

表示這條 LineRenderer 要用多少個「節點點位」來畫線。  
    
* SetPositions(Vector3[] positions)

一次性設定所有點的位置。

參數是一個 Vector3[] 陣列，長度必須等於 positionCount。

Unity 會依序把這些點連起來，畫成一條線。    
:::
    

