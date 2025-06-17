using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Player : MonoBehaviour
{
    #region "Fields/Props"

    public Transform m_SelectMarker = null;
    public Field m_Field = null;

    private Gem m_SelectGem = null;

    private Camera cam;

    private Transform m_MarkerInstance = null;

    public SoundPlayer m_SoundPlayer;

    public Transform Marker {
        get {
            if (m_MarkerInstance == null)
                m_MarkerInstance = Instantiate(m_SelectMarker);

            return m_MarkerInstance;
        }
    }

    #endregion
    #region "Functions/Methods"

    private void Awake()
    {
        cam = GameObject.FindGameObjectWithTag(
                "MainCamera").GetComponent<Camera>();
    }

    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        if (Input.GetButtonDown("Fire1"))
        {
            Collider2D col;
            Vector2 mousePos = cam.ScreenToWorldPoint(Input.mousePosition);
            
            if (col = Physics2D.OverlapPoint(
                mousePos))
            {
                Gem gem = col.transform.GetComponent<Gem>();

                if (gem != null)
                    GemPressed(gem);
            }
        }

        if (Input.GetButtonDown("Fire2"))
            UnselectGem();
    }

    public bool IsGemSelected(Gem gem)
    {
        return m_SelectGem == gem;
    }

    public void UnselectGem()
    {
        m_SelectGem = null;
        Marker.gameObject.SetActive(false);
        Marker.parent = transform;
    }

    public void SelectGem(Gem gem)
    {
        m_SelectGem = gem;

        Marker.parent = gem.GetComponentInChildren<SpriteRenderer>().transform;
        Marker.localPosition = Vector3.zero;

        if (m_SoundPlayer == null)
            return;

        m_SoundPlayer.Select();
    }

    public void GemPressed(Gem gem)
    {
        if (gem.Gathered)
            return;

        if (m_SelectGem && m_SelectGem != gem)
        {
            if (m_SelectGem.IsNear(gem))
            {
                m_Field.TrySwap(m_SelectGem, gem);
                UnselectGem();

                return;
            }
        }

        if (m_SelectGem != gem)
            SelectGem(gem);
        else
            UnselectGem();

        Marker.gameObject.SetActive(m_SelectGem != null);
    }

    #endregion
}
