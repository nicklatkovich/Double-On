using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using UnityEngine;
using KeepCoding;

public class DoubleOnModule : ModuleScript {
	public const float GRID_CELL_SIZE = 0.023f;
	public const float CONNECTION_SIZE = 0.003f;
	// public const float GRID_CELL_SIZE = 0.0115f;
	// public const float CONNECTION_SIZE = 0.0015f;
	public const float BUTTON_OFFSET_FACTOR = 0.2f;
	public const float NUM_DISPLAYS_OFFSET = 0.024f;

	private static readonly Color[] COLORS = new[] {
		Color.red,
		Color.green,
		Color.blue,
		Color.cyan,
		Color.magenta,
		Color.yellow,
		Color.black,
		Color.white,
	}.Take(DoubleOnPuzzle.COLORS_COUNT).ToArray();

	private static readonly char[] COLOR_SHORT_NAMES = "RGBCMYKW".ToArray().Take(DoubleOnPuzzle.COLORS_COUNT).ToArray();

	public readonly string TwitchHelpMessage = new[] {
		"\"!{0} read\" - show button colors in chat",
		"\"!{0} 1BG 2RR 3CM\" - press buttons of certain colors",
		"The number before colors is the number of the LED connected to the button in the reading order starting from 1",
	}.Join(" | ");

	public Material UnlitConnectionMaterial;
	public Material LitConnectionMaterial;
	public Transform GridContainer;
	public Transform NumDisplaysContainer;
	public KMSelectable Selectable;
	public KMAudio Audio;
	public ButtonComponent ButtonPrefab;
	public LEDComponent LEDPrefab;
	public NumDisplayComponent NumDisplayPrefab;
	public ConnectionComponent ConnectionPrefab;

	private string _readMessage;
	private int[] _shuffledColorInds;
	private int[] _colorIndsDecoder;
	private DoubleOnPuzzle _puzzle;
	private ButtonComponent[] _buttons;
	private ConnectionComponent[] _connections;
	private LEDComponent[] _leds;
	private NumDisplayComponent[] _numDisplays;
	private Color[] _colors;
	private int[] _colorDisplayIndex;
	private List<string> _nextLog = new List<string>();

	private void Start() {
		_shuffledColorInds = Enumerable.Range(0, DoubleOnPuzzle.COLORS_COUNT).ToArray().Shuffle();
		_colorIndsDecoder = new int[_shuffledColorInds.Length];
		for (int j = 0; j < _shuffledColorInds.Length; j++) _colorIndsDecoder[_shuffledColorInds[j]] = j;
		_colors = _shuffledColorInds.Select(j => COLORS[j]).ToArray();
		// _colors = COLORS.ToArray();
		_puzzle = new DoubleOnPuzzle();
		_buttons = new ButtonComponent[_puzzle.ButtonPositions.Length];
		_connections = new ConnectionComponent[_puzzle.ButtonPositions.Length];
		_leds = new LEDComponent[_puzzle.LEDPositions.Length];
		for (int j = 0; j < _puzzle.ButtonPositions.Length; j++) {
			Vector2Int pos = _puzzle.ButtonPositions[j];
			Vector2Int ledPos = _puzzle.LEDPositions[j / 2];
			_buttons[j] = Instantiate(ButtonPrefab);
			_buttons[j].transform.parent = GridContainer;
			Vector2 buttonPos = pos + (Vector2)(ledPos - pos) * BUTTON_OFFSET_FACTOR;
			_buttons[j].transform.localPosition = GRID_CELL_SIZE * new Vector3(buttonPos.x, 0f, -buttonPos.y);
			_buttons[j].transform.localScale = Vector3.one;
			// _buttons[j].transform.localScale = Vector3.one / 2f;
			_buttons[j].transform.localRotation = Quaternion.identity;
			_buttons[j].Color1 = _colors[_puzzle.ButtonColors[j][0]];
			_buttons[j].Color2 = _colors[_puzzle.ButtonColors[j][1]];
			_buttons[j].Selectable.Parent = Selectable;
			_connections[j] = Instantiate(ConnectionPrefab);
			_connections[j].transform.parent = GridContainer;
			Vector2 connPos = (buttonPos + ledPos) / 2f * GRID_CELL_SIZE;
			_connections[j].transform.localPosition = new Vector3(connPos.x, 0f, -connPos.y);
			_connections[j].transform.localScale = new Vector3(CONNECTION_SIZE, CONNECTION_SIZE, GRID_CELL_SIZE * (1f - BUTTON_OFFSET_FACTOR) + CONNECTION_SIZE);
			Vector3 ledPosV3 = new Vector3(ledPos.x, 0, -ledPos.y) * GRID_CELL_SIZE;
			_connections[j].transform.localRotation = Quaternion.LookRotation(_buttons[j].transform.localPosition - ledPosV3, Vector3.up);
			_connections[j].Color1 = _colors[_puzzle.ButtonColors[j][0]];
			_connections[j].Color2 = _colors[_puzzle.ButtonColors[j][1]];
		}
		for (int j = 0; j < _puzzle.LEDPositions.Length; j++) {
			_leds[j] = Instantiate(LEDPrefab);
			_leds[j].transform.parent = GridContainer;
			_leds[j].transform.localPosition = new Vector3(_puzzle.LEDPositions[j].x * GRID_CELL_SIZE, 0f, -_puzzle.LEDPositions[j].y * GRID_CELL_SIZE);
			_leds[j].transform.localScale = Vector3.one;
			_leds[j].transform.localRotation = Quaternion.identity;
		}
		Selectable.Children = _buttons.Select(b => b.Selectable).ToArray();
		Selectable.UpdateChildrenProperly();
		int[] numColors = Enumerable.Range(0, DoubleOnPuzzle.COLORS_COUNT - 1).ToArray().Shuffle();
		_numDisplays = new NumDisplayComponent[DoubleOnPuzzle.COLORS_COUNT - 1];
		_colorDisplayIndex = new int[DoubleOnPuzzle.COLORS_COUNT];
		for (int j = 0; j < DoubleOnPuzzle.COLORS_COUNT - 1; j++) {
			NumDisplayComponent numDisplay = Instantiate(NumDisplayPrefab);
			numDisplay.transform.parent = NumDisplaysContainer;
			numDisplay.transform.localPosition = Vector3.back * NUM_DISPLAYS_OFFSET * j;
			numDisplay.transform.localScale = Vector3.one;
			// numDisplay.transform.localScale = Vector3.one / 2f;
			numDisplay.transform.localRotation = Quaternion.identity;
			numDisplay.SetColor(_colors[numColors[j]]);
			numDisplay.FrontText.text = _puzzle.ColorCounts[numColors[j]].ToString().PadLeft(2, ' ');
			_numDisplays[j] = numDisplay;
			_colorDisplayIndex[numColors[j]] = j;
		}
		InitialLog();
	}

	public override void OnActivate() {
		base.OnActivate();
		for (int j = 0; j < _buttons.Length; j++) {
			int jj = j;
			_buttons[j].Selectable.OnInteract += () => { PressButton(jj); return false; };
		}

	}

	public void PressButton(int index) {
		Audio.PlayGameSoundAtTransform(KMSoundOverride.SoundEffect.ButtonPress, _buttons[index].transform);
		int ledIndex = index / 2;
		if (_puzzle.LitLEDs[ledIndex]) return;
		_nextLog.Add(string.Format("{0}:{1}", index / 2 + 1, ButtonColorsToString(index)));
		int count = _puzzle.ButtonColors[index][0] == _puzzle.ButtonColors[index][1] ? 2 : 1;
		foreach (int colorIndex in _puzzle.ButtonColors[index]) {
			if (_puzzle.LeftColorPresses[colorIndex] >= count) continue;
			SubmitLog();
			Log("{0} color limit exceeded. Strike!", COLOR_SHORT_NAMES[_shuffledColorInds[colorIndex]]);
			Strike();
			Reset();
			return;
		}
		_puzzle.Press(index);
		foreach (int colorIndex in _puzzle.ButtonColors[index]) {
			if (colorIndex < DoubleOnPuzzle.COLORS_COUNT - 1) {
				_numDisplays[_colorDisplayIndex[colorIndex]].FrontText.text = _puzzle.LeftColorPresses[colorIndex].ToString().PadLeft(2, ' ');
			}
		}
		_leds[ledIndex].SelfRenderer.material.color = Color.green;
		_connections[index].SelfRenderer.material = LitConnectionMaterial;
		_connections[index].UpdateColors();
		if (_puzzle.LitLEDs.All(l => l)) {
			SubmitLog();
			Log("Module Solved!");
			Solve();
			Audio.PlayGameSoundAtTransform(KMSoundOverride.SoundEffect.CorrectChime, transform);
		}
	}

	private void Reset() {
		_puzzle.Reset();
		foreach (LEDComponent led in _leds) led.SelfRenderer.material.color = Color.black;
		for (int j = 0; j < DoubleOnPuzzle.COLORS_COUNT - 1; j++) {
			_numDisplays[_colorDisplayIndex[j]].FrontText.text = _puzzle.ColorCounts[j].ToString().PadLeft(2, ' ');
		}
		foreach (ConnectionComponent conn in _connections) conn.SelfRenderer.material = UnlitConnectionMaterial;
	}

	public IEnumerator ProcessTwitchCommand(string command) {
		command = command.Trim().ToLower();
		if (command == "read") {
			yield return null;
			yield return "sendtochat {0}, !{1} read is: " + _readMessage;
			yield break;
		}
		if (!Regex.IsMatch(command, @"^([1-9]\d*[rgbcmy]{2}( +|$))+$")) yield break;
		string[] subCommands = command.Split(' ').Where(s => s.Length > 0).ToArray();
		int[] btnIndices = subCommands.Select(s => int.Parse(s.Take(s.Length - 2).Join("")) - 1).ToArray();
		if (new HashSet<int>(btnIndices).Count != btnIndices.Length) yield break;
		int[][] colInd = subCommands.Select(s => s.Skip(s.Length - 2).Select(c => _colorIndsDecoder[COLOR_SHORT_NAMES.IndexOf(c.ToUpper())]).ToArray()).ToArray();
		yield return null;
		if (btnIndices.Any(b => b >= _puzzle.ButtonPositions.Length)) {
			yield return "sendtochaterror {0}, !{1} invalid LED id.";
			yield break;
		}
		List<KMSelectable> result = new List<KMSelectable>();
		for (int j = 0; j < btnIndices.Length; j++) {
			if (_puzzle.LitLEDs[btnIndices[j]]) {
				yield return "sendtochaterror {0}, !{1} " + string.Format("LED #{0} already lit.", btnIndices[j] + 1);
				yield break;
			}
			int[] cls = colInd[j];
			int k = -1;
			for (int kj = 0; kj < 2; kj++) {
				int[] btnCls = _puzzle.ButtonColors[2 * btnIndices[j] + kj];
				if ((btnCls[0] == cls[0] && btnCls[1] == cls[1]) || (btnCls[0] == cls[1] && btnCls[1] == cls[0])) {
					k = kj;
					break;
				}
			}
			if (k < 0) {
				yield return new[] {
					"sendtochaterror {0}, !{1}",
					string.Format("LED #{0} has no button {1}.", btnIndices[j] + 1, subCommands[j].Skip(subCommands[j].Length - 2).Join("").ToUpper()),
				}.Join(" ");
				yield break;
			}
			result.Add(_buttons[2 * btnIndices[j] + k].Selectable);
		}
		yield return result.ToArray();
	}

	public IEnumerator TwitchHandleForcedSolve() {
		yield return null;
		for (int j = 0; j < _puzzle.LEDPositions.Length; j++) {
			if (!_puzzle.LitLEDs[j]) continue;
			if (_puzzle.Solution[j] != _puzzle.Presses[j]) {
				Reset();
				yield return new WaitForSeconds(.1f);
				break;
			}
		}
		for (int j = 0; j < _puzzle.LEDPositions.Length; j++) {
			if (_puzzle.LitLEDs[j]) continue;
			PressButton(2 * j + _puzzle.Solution[j]);
			yield return new WaitForSeconds(.1f);
		}
	}

	private void SubmitLog() {
		Log("Pressed Buttons: {0}", _nextLog.Join("; "));
		_nextLog = new List<string>();
	}

	private void InitialLog() {
		Log("Buttons:");
		List<string> rows = new List<string>();
		for (int j = 0; j < _puzzle.LEDPositions.Length; j++) {
			string row = string.Format("{0}:{1}|{2}", j + 1, ButtonColorsToString(2 * j), ButtonColorsToString(2 * j + 1));
			Log(row);
			rows.Add(row);
		}
		_readMessage = rows.Join("; ");
		Log("Limits: {0}", Enumerable.Range(0, DoubleOnPuzzle.COLORS_COUNT).Select(j => (
			string.Format("{0}:{1}", COLOR_SHORT_NAMES[_shuffledColorInds[j]], _puzzle.ColorCounts[j])
		)).Join("; "));
		Log("Hidden Limit: {0}", COLOR_SHORT_NAMES[_shuffledColorInds.Last()]);
		Log("Solution: {0}", _puzzle.SolutionLog.Select(j => string.Format("{0}:{1}", j + 1, ButtonColorsToString(2 * j + _puzzle.Solution[j]))).Join("; "));
	}

	private string ButtonColorsToString(int buttonInd) {
		return _puzzle.ButtonColors[buttonInd].Select(c => COLOR_SHORT_NAMES[_shuffledColorInds[c]]).Join("");
	}
}
