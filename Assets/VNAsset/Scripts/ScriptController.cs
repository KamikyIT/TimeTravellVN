#define DEVELOPER_MODE

using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;
using System.Collections;
using System.Collections.Generic;
using ImageDimensions;
using System.Globalization;

public interface IScriptController
{
	void Click();
	void GotoScene(string key);
	void Hide();
	void StartGame();
}

public class ScriptController : MonoBehaviour, IScriptController
{
    struct NextButton
    {
        public string text;
        public string scene;
    }

    public static ScriptController instance;

    public string debugDataFolder = @"D:\Games\TimeTravellVN\VN";

    public TextAsset txt;

    [SerializeField] Text textArea;
    [SerializeField] Text textName;
    [SerializeField] GameObject next;

    [SerializeField] RawImage[] backgrounds;
    int currentBG = 0;
    bool bgChanged = false;

    [SerializeField] GameObject portraitPrefab;
	[SerializeField] Transform portraitPanel;

	[SerializeField] GameObject optionButtonPrefab;
	[SerializeField] Transform buttonsPanel;

    string currentScene = "";

    List<string> scenes = new List<string>();
    Dictionary<string, string> dialogs = new Dictionary<string, string>();
    Dictionary<string, string> charFolders = new Dictionary<string, string>();
    Dictionary<string, Color> colors = new Dictionary<string, Color>();

    List<NextButton> buttons = new List<NextButton>();

    int narrativeTextLength;
    int narrativeTextCurrentIndex;

    Coroutine printText;

    Regex makeTextClear = new Regex("(^.*?:[\n\r]*)|({[\\s\\S]*?}\\s*)");
    Regex scriptText = new Regex("{[\\s\\S]*?}\\s*");

    string dataFolder;

	#region Unity

	void Awake()
    {
        if (instance == null)
        {
            instance = this;
        }
        else
        {
            Destroy(gameObject);
        }
    }

    void Start()
    {
        dataFolder = Directory.GetParent(Application.dataPath).FullName + "/VN/";
#if UNITY_EDITOR
        dataFolder = debugDataFolder;
        if(!dataFolder.EndsWith("/")){
            dataFolder += "/";
        }
#endif

        Hide();

		var filePath = "";

		if (txt == null)
		{
			filePath = Path.Combine(dataFolder, "script.txt");

			if (!File.Exists(filePath))
			{
				Debug.LogError("Cannot find source script file : \"" + filePath + "\"");
				return;
			}
        }

		var scriptFileText = File.ReadAllText(filePath);

		var lines = scriptFileText.Split('\n');

        var key = "";
        var phrase = new StringBuilder();

        foreach (var l in lines)
        {
            if (string.IsNullOrEmpty(l)) continue;
            if (l.StartsWith("//")) continue;

            switch (l[0])
            {
                case '@':
                    if (key.Length > 0)
                    {
                        scenes.Add(key);
                        dialogs.Add(key, phrase.ToString());
                        phrase = new StringBuilder();
                    }

                    key = l.Trim('\r', '\n');
                    break;

                case '#':
                    string[] split = l.Trim('\r', '\n').TrimEnd(')').Split('(', ',');

                    for (int i = 0; i < split.Length; i++)
                    {
                        split[i] = split[i].Trim(' ', '\t');
                    }

                    if (l.StartsWith("#color"))
                    {
						var newColor = ColorHelper.HexToColor(split[2]);

						if (!newColor.HasValue)
						{
							Debug.LogError("Cannot parse color : " + split[2] + ".\n" +
								"Making defult White color.");
							newColor = Color.white;
						}


						if (!colors.ContainsKey(split[1]))
                        {
							colors.Add(split[1], newColor.Value);
						}
						else
                        {
							colors[split[1]] = newColor.Value;
						}
                    }
                    else if (l.StartsWith("#folder"))
                    {

                        if (!charFolders.ContainsKey(split[1]))
                        {
                            charFolders.Add(split[1], split[2] + "/");
                        }
                        else
                        {
                            charFolders[split[1]] = split[2] + "/";
                        }
                    }
                    break;

                default:
                    if (key.Length > 0)
                    {
                        phrase.AppendLine(l);
                    }
                    break;
            }
        }

        scenes.Add(key);
        dialogs.Add(key, phrase.ToString());

		StartGame();
	}

    void Update()
    {
        if (buttonsPanel.gameObject.activeSelf) return;

        if (Input.GetKeyDown(KeyCode.Space))
        {
            Click();
        }
    }

	#endregion

	#region IScriptController

	public void Click()
    {
        if (buttonsPanel.gameObject.activeSelf) return;

        if (narrativeTextCurrentIndex < narrativeTextLength)
        {
            narrativeTextCurrentIndex = narrativeTextLength;
        }
        else
        {
            narrativeTextCurrentIndex = 0;

            if (buttons.Count > 0)
            {
                if (buttons.Count == 1 && string.IsNullOrEmpty(buttons[0].text))
                {
                    GotoScene(buttons[0].scene);
                }
            }
            else
            {
                int currentSceneIndex = scenes.IndexOf(currentScene);

                if(currentSceneIndex < (scenes.Count - 1)){
                    GotoScene(scenes[currentSceneIndex + 1]);
                }
            }
        }
    }

    public void Hide()
    {
        if (printText != null) StopCoroutine(printText);

        textArea.text = "";
        textName.text = "";
        textName.color = Color.white;
        next.SetActive(false);
    }

    public void GotoScene(string key)
    {
        buttons.Clear();

        for (int i = 0; i < buttonsPanel.childCount; i++)
        {
            Destroy(buttonsPanel.GetChild(i).gameObject);
        }

        buttonsPanel.gameObject.SetActive(false);

        if (dialogs.ContainsKey(key))
        {
            currentScene = key;
            Say(dialogs[key]);
        }
    }

	public void StartGame()
	{
		ScriptController.instance.GotoScene("@start");
	}

	#endregion


	void Say(string text)
    {
        if (printText != null) StopCoroutine(printText);

        textArea.text = "";
        next.SetActive(false);

        printText = StartCoroutine(PrintText(text));
    }

    void RunScriptBlock(string script)
    {
        if (string.IsNullOrEmpty(script)) return;

        script = script.Trim('\n', '\r').Trim('{', '}');

        string[] scriptLines = script.TrimStart('{').TrimEnd('}').Split('\n');

		#if DEVELOPER_MODE

		var log = new StringBuilder();
		log.Append("scriptLines.Len = " + scriptLines.Length + "\n");
		foreach (var item in scriptLines)
			log.Append(item + "\n");
		Debug.LogError(log.ToString());

		#endif

        foreach (string s in scriptLines)
        {

            string line = s.Trim('\n', '\r');

            if (!string.IsNullOrEmpty(line))
            {
                string[] procVar = line.TrimEnd(')').Split('(', ',');

                for (int i = 0; i < procVar.Length; i++)
                {
                    procVar[i] = procVar[i].Trim(' ', '\t');
                }

                if (procVar.Length > 1)
                {
					switch (procVar[0])
                    {
                        case "goto":
                            buttons.Add(new NextButton() { text = "", scene = procVar[1] });
                            break;

                        case "button":
                            procVar = line.TrimEnd(')').Split(new char[]{'(', ','}, 3);
                            buttons.Add(new NextButton() { text = procVar[2], scene = procVar[1] });
                            break;

                        case "show":

                            string on = procVar[1];

							var pos = 0f;
							
							if (procVar.Length > 3f)
								if (!float.TryParse(procVar[3], NumberStyles.Float, CultureInfo.InvariantCulture.NumberFormat, out pos))
									Debug.LogError("Cannot parse float : " + procVar[3]);

							ShowPortrait(procVar[2], on, pos);
                            break;

                        case "hide":
                            HidePortrait(procVar[1]);
                            break;

                        case "hideall":
                            for (int i = 0; i < portraitPanel.childCount; i++)
                                StartCoroutine(AutoHideImage(portraitPanel.GetChild(i).GetComponent<RawImage>()));
                            break;

                        case "bg":
                            ShowBG(procVar[1]);
                            break;

                        case "move":
							var moveValue = 0f;

							if (!float.TryParse(procVar[2], NumberStyles.Float, CultureInfo.InvariantCulture.NumberFormat, out moveValue))
								Debug.LogError("Cannot parse float : " + procVar[2]);

							MovePortrait(procVar[1], moveValue);
							break;

                        case "music":
                            SoundController.instance.PlayMusic(dataFolder + procVar[1]);
                            break;

                        case "sound":
                            SoundController.instance.PlaySound(dataFolder + procVar[1]);
                            break;

                        case "loadscene":
                            SceneManager.LoadScene(procVar[1]);
                            break;
                    }
                }
            }
        }
    }

    IEnumerator PrintText(string text)
    {
        MatchCollection script = scriptText.Matches(text);

        foreach (Match m in script)
        {
            RunScriptBlock(m.Value);
        }

        string clearText = makeTextClear.Replace(text, "");
        narrativeTextLength = clearText.Length;
        narrativeTextCurrentIndex = 0;

		string[] lines = text.Split(new char[] { '\n' }, System.StringSplitOptions.RemoveEmptyEntries);
        string nameLine = lines[0].Trim('\n', '\r');

        if (lines.Length == 1 || !nameLine.EndsWith(":"))
        {
            textName.text = "";
            textName.color = Color.white;
        }

        if (bgChanged)
        {
            bgChanged = false;
            yield return new WaitForSeconds(1);
        }

        if (lines.Length > 1 && nameLine.EndsWith(":"))
        {
            string n = nameLine.Substring(0, nameLine.Length - 1);
            textName.text = n;
            if (colors.ContainsKey(n)) textName.color = colors[n];
        }

        if (narrativeTextLength > 0)
        {
            for (; ; )
            {
                yield return null;

                string t = clearText + "</color>";
                t = t.Insert(narrativeTextCurrentIndex, "<color=#ffffff00>");

                textArea.text = t;

                if (narrativeTextCurrentIndex++ >= narrativeTextLength)
                {
                    if (buttons.Count > 0)
                    {
                        if (buttons.Count > 1 || !string.IsNullOrEmpty(buttons[0].text))
                        {
                            buttonsPanel.gameObject.SetActive(true);

                            foreach (NextButton nb in buttons)
                            {
                                GameObject b = Instantiate(optionButtonPrefab, ScriptController.instance.buttonsPanel);
                                OptionButton ob = b.GetComponent<OptionButton>();
								ob.Refresh(text: nb.text, clickCallback: () => { ScriptController.instance.GotoScene(nb.scene); });
                            }
                        }
                    }
                    break;
                }
            }
        }

        next.SetActive(true);
    }

    void MovePortrait(string name, float dest)
    {
        Transform portrait = portraitPanel.Find(Path.GetFileNameWithoutExtension(name));
        if (portrait != null)
        {
            StartCoroutine(AutoMove(portrait.GetComponent<RectTransform>(), dest));
        }
    }

    IEnumerator AutoMove(RectTransform rectTransform, float dest)
    {
        Vector2 to = new Vector2((int)(Screen.width / 2f * dest), 0);
        Vector2 from = rectTransform.anchoredPosition;

        float f = 0f;

        while (f < 1)
        {
            yield return null;

            f += Time.deltaTime;

            if(rectTransform == null) yield break;
            rectTransform.anchoredPosition = Vector2.Lerp(from, to, f);
        }

        rectTransform.anchoredPosition = to;
    }

    void HidePortrait(string name)
    {
        Transform portrait = portraitPanel.Find(Path.GetFileNameWithoutExtension(name));
        if (portrait != null)
        {
            StartCoroutine(AutoHideImage(portrait.GetComponent<RawImage>()));
        }
    }

    void ShowPortrait(string path, string showOn = "", float position = 0f)
    {
        string filename = Path.GetFileNameWithoutExtension(dataFolder + path);

        Vector2 initPosition = Vector2.zero;
        Transform on = portraitPanel.Find(Path.GetFileNameWithoutExtension(showOn));

        if (on != null && on.GetComponent<RawImage>().texture.name == filename)
        {
            MovePortrait(showOn, position);
            return;
        }

        GameObject portraitGO = Instantiate(portraitPrefab, Vector2.zero, Quaternion.identity, portraitPanel);
        if (on)
        {
            portraitGO.transform.position = on.position;
        }
        else
        {
            initPosition.x = (int)(Screen.width / 2f * position);
            portraitGO.GetComponent<RectTransform>().anchoredPosition = initPosition;
        }
        portraitGO.name = showOn;
        RawImage portrait = portraitGO.GetComponent<RawImage>();

        byte[] data = null;

        string fullPath = dataFolder + path;

        if (!File.Exists(fullPath) && charFolders.ContainsKey(showOn)) fullPath = dataFolder + charFolders[showOn] + path;

        try
        {
            data = File.ReadAllBytes(fullPath);
        }
        catch (IOException e)
        {
            print("OOPS! Error while reading image [" + fullPath + "]\n\n<size=12><color=purple>" + e.Message + "</color></size>");
            return;
        }

        ImageSize size = ImageHelper.GetDimensions(fullPath);

        portraitGO.GetComponent<RectTransform>().sizeDelta = new Vector2(size.width, size.height);

        Texture2D texture = new Texture2D(size.width, size.height, TextureFormat.ARGB32, false);
        texture.LoadImage(data);
        texture.name = filename;

        portrait.texture = texture;

        StartCoroutine(AutoShowImage(portrait, 1, on));
    }

    void ShowBG(string path)
    {
        string filename = Path.GetFileNameWithoutExtension(dataFolder + path);

        if (backgrounds[currentBG].texture != null && backgrounds[currentBG].texture.name.Equals(filename))
        {
            return;
        }
        else
        {

            bgChanged = true;

            backgrounds[currentBG].transform.SetAsFirstSibling();

            currentBG = 1 - currentBG;

            if (backgrounds[currentBG].texture != null)
            {
                Destroy(backgrounds[currentBG].texture);
            }

            byte[] data = null;

            try
            {
                data = File.ReadAllBytes(dataFolder + path);
            }
            catch (IOException e)
            {
                print("OOPS! Error while reading image [" + path + "]\n\n<size=12><color=purple>" + e.Message + "</color></size>");
                return;
            }

            ImageSize size = ImageHelper.GetDimensions(dataFolder + path);

            //backgrounds[currentBG].GetComponent<RectTransform>().sizeDelta = new Vector2(size.width, size.height);
            backgrounds[currentBG].GetComponent<AspectRatioFitter>().aspectRatio = (float)size.width / (float)size.height;

            Texture2D texture = new Texture2D(size.width, size.height, TextureFormat.ARGB32, true);
            texture.LoadImage(data);
            texture.name = filename;

            backgrounds[currentBG].texture = texture;

            StartCoroutine(AutoShowImage(backgrounds[currentBG], 2));
        }
    }

    IEnumerator AutoShowImage(RawImage image, int delay = 1, Transform on = null)
    {
        Color c = Color.white;
        c.a = 0;

        while ((c.a += Time.deltaTime * 4f) < 1f)
        {
            image.color = c;

            for (int i = 0; i < delay; i++)
            {
                yield return null;
            }
        }

        c.a = 1;
        image.color = c;

        if (on != null) StartCoroutine(AutoHideImage(on.GetComponent<RawImage>()));
    }

    IEnumerator AutoHideImage(RawImage image, int delay = 1)
    {
        Color c = Color.white;
        c.a = 1;

        while ((c.a -= Time.deltaTime * 4f) > 0)
        {
            if (image == null) yield break;
            image.color = c;

            for (int i = 0; i < delay; i++)
            {
                yield return null;
            }
        }

        Destroy(image);
        Destroy(image.gameObject);
    }
}