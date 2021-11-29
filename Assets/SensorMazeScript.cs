using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class SensorMazeScript : MonoBehaviour {

    public KMAudio Audio;
    public KMBombModule module;
    public KMColorblindMode cbmode;
    public KMSelectable modselect;
    public GameObject[] mazes;
    public Renderer[] mrends;
    public List<KMSelectable> mazewall;
    public List<KMSelectable> buttons;
    public Renderer[] bulbs;
    public TextMesh[] cbtext;
    public Material[] glasscols;
    public Material[] statuscols;
    public Light[] lights;
    public GameObject matstore;

    private static string[] dcols = new string[10] { "Orange", "Yellow", "Jade", "Cyan", "Blue", "Violet", "Magenta", "White", "Grey", "Brown"};
    private static string[] dord = new string[4] { "Top-Left", "Top-Right", "Bottom-Left", "Bottom-Right" };
    private int[] order = new int[4] { 0, 1, 2, 3};
    private static int[,,] lpair = new int[2, 6, 2]
    {
        { { 0, 2}, { 0, 1}, { 2, 3}, { 1, 3}, { 0, 3}, { 1, 2} },
        { { 0, 2}, { 0, 3}, { 0, 1}, { 1, 3}, { 1, 2}, { 2, 3} }
    };
    private List<int> rcols;
    private int[] miscols;
    private static int[,] coltable = new int[10, 10]
    {
        { -1, 3, 1, 0, 2, 1, 0, 3, 2, 1},
        { 3, -1, 2, 3, 1, 0, 2, 1, 3, 0},
        { 1, 2, -1, 1, 3, 1, 0, 2, 0, 2},
        { 0, 3, 1, -1, 0, 2, 3, 1, 2, 0},
        { 2, 1, 3, 0, -1, 3, 1, 0, 1, 2},
        { 1, 0, 1, 2, 3, -1, 0, 1, 2, 3},
        { 0, 2, 0, 3, 1, 0, -1, 2, 0, 1},
        { 3, 1, 2, 1, 0, 1, 2, -1, 3, 0},
        { 2, 3, 0, 2, 1, 2, 0, 3, -1, 1},
        { 1, 0, 2, 0, 2, 3, 1, 0, 1, -1}
    };
    private int[][] lcols = new int[4][] { new int[4], new int[4], new int[4], new int[4]};
    private int progress;
    private bool restart;
    private bool cb;
    private static int moduleIDCounter;
    private int moduleID;

    private void OnApplicationFocus(bool focus)
    {
        if (!focus && !restart && progress > 0 && progress < 4)
            StartCoroutine(Restart(true));
    }

    private void Start()
    {
        moduleID = ++moduleIDCounter;
        cb = cbmode.ColorblindModeActive;
        matstore.SetActive(false);
        float scale = transform.lossyScale.x;
        modselect.OnDefocus = delegate () { if (!restart && progress > 0 && progress < 4) StartCoroutine(Restart(true)); };
        foreach (Light l in lights)
        {
            l.range *= scale;
            l.enabled = false;
        }
        foreach (Renderer m in mrends)
            m.enabled = false;
        foreach (KMSelectable l in buttons)
        {
            int b = buttons.IndexOf(l);
            l.OnHighlight = delegate () { HL(b); };
            l.OnHighlightEnded = delegate ()
            {
                if (progress < 1 && !restart)
                    for (int i = 0; i < 4; i++)
                    {
                        if (cb)
                            cbtext[i].text = string.Empty;
                        bulbs[i].material = statuscols[0];
                        lights[i].enabled = false;
                    }
            };
            l.OnInteract = delegate () { Press(b); return false; };
        }
        foreach (KMSelectable m in mazewall)
            m.OnHighlight = delegate () { if (!restart && progress > 0 && progress < 4) { mrends[miscols[2] + 1].enabled = true; StartCoroutine(Restart(false)); } };
        mrends[0].enabled = true;
        foreach (GameObject m in mazes)
            m.SetActive(false);
        StartOver();
    }

    private void StartOver()
    {
        order.Shuffle();
        rcols = new List<int> { 0, 1, 2, 3, 4, 5, 6, 7, 8, 9 }.Shuffle().ToList();       
        miscols = rcols.Take(3).ToArray();
        rcols = rcols.Skip(3).ToList();
        rcols = rcols.Concat(rcols).ToList().Shuffle();
        int third = coltable[miscols[0], miscols[1]];
        while (order[2] != third)
            order.Shuffle();
        int[,] cpair = new int[2, 2];
        for (int i = 0; i < 2; i++)
        {
            int c = (order[i] + (2 * i) + (order[i] < 2 && Random.Range(0, 2) == 0 ? 4 : 0)) % 6;
            for (int j = 0; j < 2; j++)
                cpair[i, j] = lpair[i, c, j];
        }
        for(int i = 0; i < 4; i++)
        {
            int[] colselect = new int[4] { -1, -1, -1, -1};
            for(int j = 0; j < 4; j++)
            {
                if (i == cpair[0, 0] && j == cpair[1, 0])
                    colselect[j] = miscols[0];
                else if (i == cpair[0, 1] && j == cpair[1, 1])
                    colselect[j] = miscols[1];
                else
                    colselect[j] = rcols[colselect.Any(x => miscols.Contains(x)) ? j - 1 : j];
            }
            lcols[i] = colselect;
            for (int j = 0; j < 4; j++)
                if (miscols.Contains(colselect[j]))
                    continue;
                else
                    rcols.Remove(colselect[j]);
        }
        Debug.LogFormat("[Sensor Maze #{0}] The unique colours are {1} and {2}, which appear on the {3} and {4} lights when the {5} and {6} lights are highlighted.", moduleID, dcols[miscols[0]], dcols[miscols[1]], dord[cpair[1, 0]], dord[cpair[1, 1]], dord[cpair[0, 0]], dord[cpair[0, 1]]);
        Debug.LogFormat("[Sensor Maze #{0}] The missing/maze colour is {1}.", moduleID, dcols[miscols[2]]);
        Debug.LogFormat("[Sensor Maze #{0}] Press the lights in the order: {1}", moduleID, string.Join(", ", order.Select(x => dord[x]).ToArray()));
    }

    private void HL(int b)
    {
        if (progress < 1 && !restart)
        {
            for (int i = 0; i < 4; i++)
            {
                if(cb)
                     cbtext[i].text = "OYJCBVMWEN"[lcols[b][i]].ToString();
                bulbs[i].material = glasscols[lcols[b][i]];
                lights[i].enabled = true;
                lights[i].color = new Color32[] { new Color32(255, 50, 0, 255), new Color32(255, 255, 0, 255), new Color32(0, 255, 70, 255), new Color32(0, 255, 255, 255), new Color32(0, 20, 255, 255), new Color32(50, 0, 255, 255), new Color32(255, 0, 255, 255), new Color32(255, 255, 255, 255), new Color32(70, 70, 70, 255), new Color32(70, 40, 0, 255)}[lcols[b][i]];
            }
        }
    }

    private void Press(int b)
    {
        if (!restart)
        {
            if (progress == 0)
                for (int i = 0; i < 4; i++)
                {
                    if (cb)
                        cbtext[i].text = string.Empty;
                    bulbs[i].material = statuscols[0];
                    lights[i].enabled = false;
                }
            if (b == order[progress])
            {
                Audio.PlaySoundAtTransform("Bulb" + progress, buttons[b].transform);
                lights[b].enabled = true;
                lights[b].color = new Color(0, 1, 0);
                bulbs[b].material = statuscols[4];
                if (progress == 0)
                {
                    Audio.PlaySoundAtTransform("Activate", transform);
                    mazes[0].SetActive(true);
                    mazes[miscols[2] + 1].SetActive(true);
                }
                progress++;
                if (progress > 3)
                {
                    module.HandlePass();
                    buttons[b].AddInteractionPunch();
                    mazes[0].SetActive(false);
                    mazes[miscols[2] + 1].SetActive(false);
                }
            }
            else
            {
                bulbs[b].material = statuscols[3];
                lights[b].enabled = true;
                lights[b].color = new Color(1, 0, 0);
                StartCoroutine(Restart(false));
            }
        }
    }

    private IEnumerator Restart(bool goofed)
    {
        restart = true;
        module.HandleStrike();
        if (goofed)
        {
            modselect.AddInteractionPunch(5);
            Audio.PlaySoundAtTransform("Alarm", transform);
            mrends[0].material = statuscols[2];
            for (int i = 0; i < 4; i++)
            {
                bulbs[i].material = statuscols[3];
                lights[i].enabled = true;
                lights[i].color = new Color(1, 0, 0);
            }
            yield return new WaitForSeconds(60);
            mrends[0].material = statuscols[1];
        }
        else
            yield return new WaitForSeconds(1);
        for(int i = 0; i < 4; i++)
        {
            lights[i].enabled = false;
            bulbs[i].material = statuscols[0];
        }
        mazes[0].SetActive(false);
        mrends[miscols[2] + 1].enabled = false;
        mazes[miscols[2] + 1].SetActive(false);
        progress = 0;
        restart = false;
        StartOver();
    }
}
