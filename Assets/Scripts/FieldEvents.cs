using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

public class FieldEvents : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler
{
    public Vector2Int coordinates = new Vector2Int(-1, -1); // Coordinates (-1 .. not assigned)
    public Figure currentFigure;
    private bool isDragging = false;

    public Image background;
    private Color32 dragColor = new Color32(0, 255, 0, 60);
    private Color32 offColor = new Color32(255, 255, 255, 0);

    public Image figureImage;
    private Color32 figWhiteColor = new Color32(190, 190, 190, 255);
    private Color32 figBlackColor = new Color32(0, 0, 0, 255);
    private Color32 figOffColor = new Color32(255, 255, 255, 0);

    private GameMgr gameMgr;



    private void Awake()
    {
        gameMgr = GameMgr.instance;
    }

    public void SetFigure(Figure newFigure)
    {
        currentFigure = newFigure;

        if (currentFigure != null)
        {
            figureImage.sprite = currentFigure.sprite;
            if (currentFigure.color == -1)
            {
                if (currentFigure.type != FigureType.knight) figureImage.color = figWhiteColor;
                else figureImage.color = new Color32(255, 255, 255, 255);
            }
            else if (currentFigure.color == 1)
            {
                if (currentFigure.type != FigureType.knight) figureImage.color = figBlackColor;
                else figureImage.color = new Color32(255, 255, 255, 255);
            }
        }
        else
        {
            figureImage.sprite = null;
            figureImage.color = figOffColor;
        }
    }

    public void ShowDragColor(bool show)
    {
        if (show) background.color = dragColor;
        else background.color = offColor;
    }


    #region DragEvents
    public void OnBeginDrag(PointerEventData e)
    {
        if (currentFigure == null) return;

        if(gameMgr.BeginDrag(this))
        {
            figureImage.color = figOffColor;
            isDragging = true;
        }
    }

    public void OnDrag(PointerEventData e)
    {
        if (!isDragging) return;

        gameMgr.UpdateDragFigurePos(e.position);
    }

    public void OnEndDrag(PointerEventData e)
    {
        if (!isDragging) return;

        if (gameMgr.EndDrag(this, e.pointerEnter.GetComponent<FieldEvents>()))
        {
            SetFigure(null);
        }
        else SetFigure(currentFigure);

        isDragging = false;
    }
    #endregion
}
