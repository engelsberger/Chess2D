using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;
using TMPro;


public enum FigureType
{
    none,
    pawn,
    knight,
    bishop,
    rook,
    queen,
    king
}

public class Figure
{
    public FigureType type;
    public Sprite sprite;
    public int color;  // -1 .. white, 0 .. not set, 1 .. black
    public bool startPosition;

    public Figure(FigureType type, Sprite sprite, int color, bool startPosition)
    {
        this.type = type;
        this.sprite = sprite;
        this.color = color;
        this.startPosition = startPosition;
    }
}


public class GameMgr : MonoBehaviour
{
    // Singleton
    public static GameMgr instance;

    // Field preset
    public GameObject fieldPreset;
    public Transform fieldParent;

    //Victory screen
    public GameObject victoryScreen;
    public TMP_Text victoryColor;
    public Button btn_reset;

    // Figure images
    public RectTransform dragImage;
    public Sprite pawn;
    public Sprite knight;
    public Sprite knight_white;
    public Sprite bishop;
    public Sprite rook;
    public Sprite queen;
    public Sprite king;

    // Variables
    private int[][] field;
    private int turn = 0;
    private List<Vector2Int> possibleFields = new List<Vector2Int>(); // contains all x,y coordinates of possible fields to move on



    private void Awake()
    {
        instance = this;
        dragImage.gameObject.SetActive(false);
        btn_reset.onClick.AddListener(ResetFigures);

        BuildBoard();
        ResetFigures();
    }


    #region Gameplay
    public bool BeginDrag(FieldEvents e)
    {
        int x = e.coordinates.x, y = e.coordinates.y;
        int c = e.currentFigure.color;

        if (turn == 0) return false;
        if (turn != c) 
        {
            ConsoleMgr.instance.Log(c == -1 ? "Not whites turn!" : "Not blacks turn!");
            return false;
        }

        // Set drag image
        SetDragFigure(e.currentFigure, e.figureImage.color);

        possibleFields.Clear();

        // Get all possible fields for the given figure
        // Pawn can move only one straight, exceptions are when he can strike someone one diagonal forward or when the game begins (2 forward)
        if(e.currentFigure.type == FigureType.pawn)
        {
            if (y + c >= 0 && y + c <= 7)
            {
                if (field[y + c][x] == 0) possibleFields.Add(new Vector2Int(x, y + c));
                if (e.currentFigure.startPosition == true && field[y + (2 * c)][x] == 0) possibleFields.Add(new Vector2Int(x, y + (2 * c)));
                if (x > 0 && field[y + c][x - 1] == -c) possibleFields.Add(new Vector2Int(x - 1, y + c));
                if (x < 7 && field[y + c][x + 1] == -c) possibleFields.Add(new Vector2Int(x + 1, y + c));
            }
        }
        // Knight moves an L shape (two in a direction and one to the side) and can jump figures
        else if(e.currentFigure.type == FigureType.knight)
        {
            if (y + 2 <= 7 && x - 1 >= 0 && field[y + 2][x - 1] != c) possibleFields.Add(new Vector2Int(x - 1, y + 2));
            if (y + 2 <= 7 && x + 1 <= 7 && field[y + 2][x + 1] != c) possibleFields.Add(new Vector2Int(x + 1, y + 2));
            if (y + 1 <= 7 && x + 2 <= 7 && field[y + 1][x + 2] != c) possibleFields.Add(new Vector2Int(x + 2, y + 1));
            if (y - 1 >= 0 && x + 2 <= 7 && field[y - 1][x + 2] != c) possibleFields.Add(new Vector2Int(x + 2, y - 1));
            if (y - 2 >= 0 && x + 1 <= 7 && field[y - 2][x + 1] != c) possibleFields.Add(new Vector2Int(x + 1, y - 2));
            if (y - 2 >= 0 && x - 1 >= 0 && field[y - 2][x - 1] != c) possibleFields.Add(new Vector2Int(x - 1, y - 2));
            if (y - 1 >= 0 && x - 2 >= 0 && field[y - 1][x - 2] != c) possibleFields.Add(new Vector2Int(x - 2, y - 1));
            if (y + 1 <= 7 && x - 2 >= 0 && field[y + 1][x - 2] != c) possibleFields.Add(new Vector2Int(x - 2, y + 1));
        }
        // Bishop moves diagonally
        else if(e.currentFigure.type == FigureType.bishop)
        {
            for(int d = 0; d < 4; d++)
            {
                int i = y + (d < 2 ? -1 : 1);
                int j = x + (d % 2 == 0 ? -1 : 1);

                while(i <= 7 && i >= 0 && j <= 7 && j >= 0)
                {
                    if (field[i][j] == 0 || field[i][j] == -c) possibleFields.Add(new Vector2Int(j, i));
                    if (field[i][j] != 0) break;

                    i += d < 2 ? -1 : 1;
                    j += d % 2 == 0 ? -1 : 1;
                }
            }
        }
        // Rook moves horizontally or vertically
        else if(e.currentFigure.type == FigureType.rook)
        {
            for (int d = 0; d < 4; d++)
            {
                int i = y + (d % 2 == 0 ? 0 : d < 2 ? 1 : -1);
                int j = x + (d % 2 == 1 ? 0 : d < 2 ? 1 : -1);

                while (i <= 7 && i >= 0 && j <= 7 && j >= 0)
                {
                    if (field[i][j] == 0 || field[i][j] == -c) possibleFields.Add(new Vector2Int(j, i));
                    if (field[i][j] != 0) break;

                    i += d % 2 == 0 ? 0 : d < 2 ? 1 : -1;
                    j += d % 2 == 1 ? 0 : d < 2 ? 1 : -1;
                }
            }
        }
        // Queen moves horizontally, vertically and diagonally
        else if(e.currentFigure.type == FigureType.queen)
        {
            // Diagonally
            for (int d = 0; d < 4; d++)
            {
                int i = y + (d < 2 ? -1 : 1);
                int j = x + (d % 2 == 0 ? -1 : 1);

                while (i <= 7 && i >= 0 && j <= 7 && j >= 0)
                {
                    if (field[i][j] == 0 || field[i][j] == -c) possibleFields.Add(new Vector2Int(j, i));
                    if (field[i][j] != 0) break;

                    i += d < 2 ? -1 : 1;
                    j += d % 2 == 0 ? -1 : 1;
                }
            }
            // Horizontally
            for (int d = 0; d < 4; d++)
            {
                int i = y + (d % 2 == 0 ? 0 : d < 2 ? 1 : -1);
                int j = x + (d % 2 == 1 ? 0 : d < 2 ? 1 : -1);

                while (i <= 7 && i >= 0 && j <= 7 && j >= 0)
                {
                    if ((field[i][j] == 0 || field[i][j] == -c)) possibleFields.Add(new Vector2Int(j, i));
                    if (field[i][j] != 0) break;

                    i += d % 2 == 0 ? 0 : d < 2 ? 1 : -1;
                    j += d % 2 == 1 ? 0 : d < 2 ? 1 : -1;
                }
            }
        }
        // King moves only one in each direction
        else if(e.currentFigure.type == FigureType.king)
        {
            for(int i = y > 0 ? y - 1 : y; i <= (y < 7 ? y + 1 : y); i++)
            {
                for (int j = x > 0 ? x - 1 : x; j <= (x < 7 ? x + 1 : x); j++)
                {
                    if (y == 0 && x == 0) continue;
                    if (field[i][j] != c) possibleFields.Add(new Vector2Int(j, i));
                }
            }
        }

        ShowPossibleFields();

        return true;
    }

    public bool EndDrag(FieldEvents oldField, FieldEvents newField)
    {
        SetDragFigure(null, Color.clear);

        if (newField == null)
        {
            possibleFields.Clear();
            ShowPossibleFields();
            return false;
        }

        bool moveOK = false; ;
        if (possibleFields.Contains(newField.coordinates))
        {
            Figure strikedFigure = newField.currentFigure;

            newField.SetFigure(oldField.currentFigure);
            field[oldField.coordinates.y][oldField.coordinates.x] = 0;
            field[newField.coordinates.y][newField.coordinates.x] = newField.currentFigure.color;

            if (newField.currentFigure.startPosition) newField.currentFigure.startPosition = false;

            if (strikedFigure != null)
            {
                if (strikedFigure.type == FigureType.king) { SetVictoryScreen(oldField.currentFigure.color); return true; }

                ConsoleMgr.instance.Log((newField.currentFigure.color == 1 ? "Black" : "White") + " " + newField.currentFigure.type.ToString() + " strikes "
                + (strikedFigure.color == 1 ? "black" : "white") + " " + strikedFigure.type.ToString() + " at field "
                + ((char)(newField.coordinates.x + 'A')) + (8 - newField.coordinates.y).ToString() + ". " + (newField.currentFigure.color == -1 ? "Blacks" : "Whites") + " turn.");
            }
            else
            {
                ConsoleMgr.instance.Log("Move " + (newField.currentFigure.color == 1 ? "black" : "white") + " " + newField.currentFigure.type.ToString() + " to field "
                + ((char)(newField.coordinates.x + 'A')) + (8 - newField.coordinates.y).ToString() + ". " + (newField.currentFigure.color == -1 ? "Blacks" : "Whites") + " turn.");
            }

            turn = -newField.currentFigure.color;
            moveOK = true;
        }

        possibleFields.Clear();
        ShowPossibleFields();

        return moveOK;
    }
    #endregion

    #region Others
    private void ShowPossibleFields()
    {
        foreach (Transform field in fieldParent)
        {
            FieldEvents f = field.GetComponent<FieldEvents>();

            if (possibleFields.Contains(f.coordinates)) f.ShowDragColor(true);
            else f.ShowDragColor(false);
        }
    }

    private void SetDragFigure(Figure figure, Color32 color)
    {
        if(figure != null)
        {
            dragImage.gameObject.SetActive(true);
            Image img = dragImage.GetComponent<Image>();
            img.sprite = figure.sprite;
            img.color = color;
        }
        else
        {
            dragImage.gameObject.SetActive(false);
        }
    }

    public void UpdateDragFigurePos(Vector2 pos)
    {
        dragImage.position = pos;
    }
    #endregion


    #region Set up game
    private void SetVictoryScreen(int color)
    {
        victoryScreen.SetActive(true);
        if (color == -1) victoryColor.text = "White";
        else victoryColor.text = "Black";
        ConsoleMgr.instance.Log((color == -1 ? "White" : "Black") + " wins!");
    }

    private void BuildBoard()
    {
        for (int i = 0; i < 8; i++)
        {
            for(int j = 0; j < 8; j++)
            {
                // Create field and set coordinates
                FieldEvents newField = Instantiate(fieldPreset, fieldParent).GetComponent<FieldEvents>();
                newField.coordinates = new Vector2Int(j, i);
                newField.ShowDragColor(false);
                newField.SetFigure(null);
            }
        }

        ConsoleMgr.instance.Log("Board successfully built.");
    }

    private void ResetFigures()
    {
        victoryScreen.SetActive(false);
        turn = -1;
        possibleFields.Clear();
        ShowPossibleFields();

        field = new int[][] {
            new int[] { 1, 1, 1, 1, 1, 1, 1, 1 },
            new int[] { 1, 1, 1, 1, 1, 1, 1, 1 },
            new int[] { 0, 0, 0, 0, 0, 0, 0, 0 },
            new int[] { 0, 0, 0, 0, 0, 0, 0, 0 },
            new int[] { 0, 0, 0, 0, 0, 0, 0, 0 },
            new int[] { 0, 0, 0, 0, 0, 0, 0, 0 },
            new int[] { -1, -1, -1, -1, -1, -1, -1, -1 },
            new int[] { -1, -1, -1, -1, -1, -1, -1, -1 }
        };

        foreach (Transform field in fieldParent)
        {
            FieldEvents mgr = field.GetComponent<FieldEvents>();
            Vector2 f = mgr.coordinates;
            mgr.SetFigure(null);

            // Black figures
            if (f.y == 0 && (f.x == 0 || f.x == 7)) mgr.SetFigure(new Figure(FigureType.rook, rook, 1, true));
            if (f.y == 0 && (f.x == 1 || f.x == 6)) mgr.SetFigure(new Figure(FigureType.knight, knight, 1, true));
            if (f.y == 0 && (f.x == 2 || f.x == 5)) mgr.SetFigure(new Figure(FigureType.bishop, bishop, 1, true));
            if (f.y == 0 && f.x == 3) mgr.SetFigure(new Figure(FigureType.queen, queen, 1, true));
            if (f.y == 0 && f.x == 4) mgr.SetFigure(new Figure(FigureType.king, king, 1, true));
            if (f.y == 1) mgr.SetFigure(new Figure(FigureType.pawn, pawn, 1, true));

            // White figures
            if (f.y == 7 && (f.x == 0 || f.x == 7)) mgr.SetFigure(new Figure(FigureType.rook, rook, -1, true));
            if (f.y == 7 && (f.x == 1 || f.x == 6)) mgr.SetFigure(new Figure(FigureType.knight, knight_white, -1, true));
            if (f.y == 7 && (f.x == 2 || f.x == 5)) mgr.SetFigure(new Figure(FigureType.bishop, bishop, -1, true));
            if (f.y == 7 && f.x == 3) mgr.SetFigure(new Figure(FigureType.queen, queen, -1, true));
            if (f.y == 7 && f.x == 4) mgr.SetFigure(new Figure(FigureType.king, king, -1, true));
            if (f.y == 6) mgr.SetFigure(new Figure(FigureType.pawn, pawn, -1, true));
        }

        ConsoleMgr.instance.Log("Board reset to default. White begins.");
    }
    #endregion
}
