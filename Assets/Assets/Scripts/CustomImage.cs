using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Sprites;

[AddComponentMenu("UI/Custom Image", 10)]
public class CustomImage : MaskableGraphic
{
    [SerializeField]
    private Sprite _sprite;

    [SerializeField]
    private Material _customMaterial;

    public Sprite sprite
    {
        get { return _sprite; }
        set
        {
            if (_sprite != value)
            {
                _sprite = value;
                SetAllDirty();
            }
        }
    }

    public Material customMaterial
    {
        get { return _customMaterial; }
        set
        {
            if (_customMaterial != value)
            {
                _customMaterial = value;
                SetMaterialDirty();
            }
        }
    }

    public override Texture mainTexture
    {
        get
        {
            if (sprite == null)
                return s_WhiteTexture;
            return sprite.texture;
        }
    }

    protected override void OnPopulateMesh(VertexHelper vh)
    {
        if (sprite == null)
        {
            base.OnPopulateMesh(vh);
            return;
        }

        vh.Clear();

        // Получаем границы спрайта
        Vector4 outer = DataUtility.GetOuterUV(sprite);
        Vector2 size = sprite.rect.size;
        Vector2 pivot = sprite.pivot / size;
        Rect rect = GetPixelAdjustedRect();

        // Вычисляем вершины
        Vector2 posMin = new Vector2(rect.xMin, rect.yMin);
        Vector2 posMax = new Vector2(rect.xMax, rect.yMax);

        // Учитываем pivot
        posMin += (Vector2.one - pivot) * rect.size;
        posMax -= pivot * rect.size;

        // Добавляем вершины
        vh.AddVert(new Vector3(posMin.x, posMin.y), color, new Vector2(outer.x, outer.y));
        vh.AddVert(new Vector3(posMin.x, posMax.y), color, new Vector2(outer.x, outer.w));
        vh.AddVert(new Vector3(posMax.x, posMax.y), color, new Vector2(outer.z, outer.w));
        vh.AddVert(new Vector3(posMax.x, posMin.y), color, new Vector2(outer.z, outer.y));

        // Добавляем треугольники
        vh.AddTriangle(0, 1, 2);
        vh.AddTriangle(2, 3, 0);
    }

    public override Material material
    {
        get
        {
            if (_customMaterial != null)
                return _customMaterial;
            return base.material;
        }
        set
        {
            base.material = value;
        }
    }
}