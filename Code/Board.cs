using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class Board : MonoBehaviour
{
    // [SerializeField] private Slider _rowsSlider;
    // [SerializeField] private Slider _thicknessSlider;
    // [SerializeField, HideInInspector] private Slider _offset;
    // [SerializeField, HideInInspector] private Toggle _enable3D;

    [SerializeField] private GameObject _planePrefab;
    [SerializeField] private GameObject _borderPrefab;
    [SerializeField, HideInInspector] private GameObject _hitBoxPrefab;
  //  [SerializeField] private bool _buildOnAwake = false;

    private int _lastRows = 3;
    private float _lastThickness = 0.5f;
    private float _lastOffset = 2.0f;
    private Dictionary<HitBox, string> _fields = new Dictionary<HitBox, string>();

    void Awake()
    {
        GameManager.Instance.SetBoard(this);
        // _rowsSlider.onValueChanged.AddListener(OnSliderValueChanged);
        // _thicknessSlider.onValueChanged.AddListener(OnSliderValueChanged);
        // _offset.onValueChanged.AddListener(OnSliderValueChanged);
        // _enable3D.onValueChanged.AddListener(b => {
        //     _offset.gameObject.SetActive(b);
        //     OnSliderValueChanged();
        // });

        // _offset.gameObject.SetActive(_enable3D.isOn);

        //Generate((int) _rowsSlider.value, _thicknessSlider.value, _offset.value);
        // ❌ 不要一進場就產生
        //if (_buildOnAwake)
           // BuildBoard(); 
        //Generate(_lastRows, _lastThickness, _lastOffset);
    }
    public void BuildBoard()
    {
        Generate(_lastRows, _lastThickness, _lastOffset);
    }


    public void Reset() {
        Generate(_lastRows, _lastThickness, _lastOffset);
    }
    public void Generate(int rows, float thickness, float offset) {
        transform.Clear();

        // 直接設定成 3D 棋盤
        GameManager.Instance.Set(rows, true);   // 第二個參數固定 true
        GameManager.Instance.Clear();

        // 一定要建立 rows 個平面 (Z 軸 = "row" index)
        for (int i = 0; i < rows; i++) {
            var parent = Instantiate(_planePrefab, transform);
            var parentPosition = parent.transform.position;

            // 讓多層棋盤沿 Y 軸置中排列
            var centeredIndex = (rows - 1f) / 2f * -1f + i;
            parentPosition.y += centeredIndex * offset;
            parent.transform.position = parentPosition;

            GenerateField(rows, thickness, parent, i);
        }
    }

    private void GenerateField(int rows, float thickness, GameObject parent, int row) {
        var size = Vector3.Scale(parent.GetComponent<MeshFilter>().mesh.bounds.size,
            parent.transform.localScale);

        // generate borders
        for (int i = 1; i < rows; i++) {
            var totalSize = size.x - thickness * (rows - 1);
            var offset = totalSize / rows;
            var position = offset * i + thickness * i - thickness / 2.0f - size.x / 2.0f;

            var borderX = Instantiate(_borderPrefab, parent.transform);
            borderX.transform.localScale = new Vector3(thickness, thickness, size.x);
            borderX.transform.localPosition = new Vector3(position, thickness / 2, 0);

            var borderY = Instantiate(_borderPrefab, parent.transform);
            borderY.transform.localScale = new Vector3(size.x, thickness, thickness);
            borderY.transform.localPosition = new Vector3(0, thickness / 2, position);
        }

        // generate hitboxes
        for (int i = 0; i < rows; i++) {
            for (int j = 0; j < rows; j++) {
                var totalSize = size.x - thickness * (rows - 1);
                var offset = totalSize / rows;
                var positionX = thickness * i + offset * i + offset / 2.0f - size.x / 2.0f;
                var positionY = thickness * j + offset * j + offset / 2.0f - size.x / 2.0f;

                var hitbox = Instantiate(_hitBoxPrefab, parent.transform);
                var localScale = new Vector3(offset, thickness, offset);
                hitbox.transform.localScale = localScale;
                hitbox.transform.localPosition = new Vector3(positionX, localScale.y / 2, positionY);

                GameManager.Instance.AddHitBox(hitbox.GetComponent<HitBox>(), i, j, row);
            }
        }
    }

}