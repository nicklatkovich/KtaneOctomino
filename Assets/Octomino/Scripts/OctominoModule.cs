using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using KModkit;
using UnityEngine;
using wawa.Modules;

public class OctominoModule : ModdedModule {

	public const int STAGES_COUNT = 5;
	public const float TIME_TO_SOLVE = 90; // seconds
	public const float RESETTING_TIME = 2f;

	private static readonly Color PURPLE = new Color32(0x7f, 0, 0xff, 0xff);

	private enum STATUS {
		CREATED = 0,
		GENERATED = 1,
		STARTED = 2,
		SOLVED = 3,
	}

	private enum OFFSET_TYPE {
		NO_OFFSET = 0,
		STRIKES_COUNT = 1,
		SOLVES_COUNT = 2,
		PORTS_COUNT = 3,
		INDICATORS_COUNT = 4,
		BATTERIES_COUNT = 5,
		STAGE_NUMBER = 6,
		LAST_SN_DIGIT = 7,
		MODULES_COUNT = 8,
	}

	private static readonly ReadOnlyCollection<OFFSET_TYPE> ALL_OFFSET_TYPES = System.Array.AsReadOnly(new OFFSET_TYPE[] {
		OFFSET_TYPE.STRIKES_COUNT,
		OFFSET_TYPE.SOLVES_COUNT,
		OFFSET_TYPE.PORTS_COUNT,
		OFFSET_TYPE.INDICATORS_COUNT,
		OFFSET_TYPE.BATTERIES_COUNT,
		OFFSET_TYPE.STAGE_NUMBER,
		OFFSET_TYPE.LAST_SN_DIGIT,
		OFFSET_TYPE.MODULES_COUNT,
	});

	private static readonly ReadOnlyCollection<Color> OFFSET_TYPES_COLORS = System.Array.AsReadOnly(new Color[] {
		Color.black,
		Color.red,
		Color.green,
		Color.blue,
		Color.cyan,
		Color.magenta,
		Color.yellow,
		Color.white,
		PURPLE,
	});

	// self components
	public KMSelectable Selectable;
	public KMAudio Audio;
	public KMBombInfo BombInfo;

	public ButtonComponent ButtonPrefab;

	public Transform ButtonsContainer;
	public OctominoTimer Timer;
	public MonoBehaviour OctominoRendererComponent;
	public StageDisplay StageDisplay;
	public RotationButton RotationButton;

	public OctominoRenderer2D OctominoRenderer2D;
	public OctominoRenderer3D OctominoRenderer3D;
	public OctominoRenderer4D OctominoRenderer4D;

	private const float BUTTONS_OFFSET = .006f;
	private const float SECONDS_BEFORE_ACTIVATION = 6f; // module's timer animation duration before bomb activation

	// sounds
	private const string START_SOUND = "OctominoStart";
	private const string MOVE_SOUND = "OctominoMove";
	private const string RESTART_SOUND = "OctominoRestart";
	private const string CORRECT_SOUND = "Correct";

	private readonly int[] STAGES_DIMS = new int[] { 2, 3, 4, 3, 2 };
	// private readonly int[] STAGES_DIMS = new int[] { 3, 3, 3, 3, 3 };
	// private readonly int[] STAGES_DIMS = new int[] { 4, 4, 4, 4, 4 };

	private readonly Color[] _colors = new Color[] {
		Color.red,
		Color.green,
		Color.blue,
		Color.cyan,
		Color.magenta,
		Color.yellow,
		Color.white,
		PURPLE, // purple
	};

	private Vector2 _mouseRotationPosition;
	private bool _isRotating = false;
	private float _resettingTime;
	private bool _isResetting = false;
	private int _finishButtonIndex;
	private int _currentButtonIndex;
	private int _passedStagesCount;
	private int _offsetDirection;
	private STATUS _status = STATUS.CREATED;
	private float _solvingTimer = 0f;
	private float _timeCheckPoint = 0f;
	private string _path;
	private bool[] _isVisited;
	private int[] _buttonToFaceIndex;
	private int[] _faceToButtonIndex;
	private OFFSET_TYPE _offset0 = OFFSET_TYPE.NO_OFFSET;
	private OFFSET_TYPE _offset1 = OFFSET_TYPE.NO_OFFSET;
	private OFFSET_TYPE _offset2 = OFFSET_TYPE.NO_OFFSET;
	private Queue<OFFSET_TYPE> _offsetTypesQueue;
	private OctominoUtils.FaceProps[] _puzzle;
	private ButtonComponent[] _buttons;

	protected void Start() {
		CreateButtons();
		StageDisplay.OuterColor_0 = OFFSET_TYPES_COLORS.PickRandom();
		StageDisplay.OuterColor_1 = OFFSET_TYPES_COLORS.PickRandom();
		StageDisplay.OuterColor_2 = OFFSET_TYPES_COLORS.PickRandom();
		StageDisplay.OuterColorsCount = Random.Range(0, 2) == 0 ? 2 : 3;
		ResetOffsetTypesQueue();
		GeneratePuzzle(STAGES_DIMS[0]);
		RotationButton.Selectable.OnInteract += () => {
			_isRotating = true;
			_mouseRotationPosition = new Vector2(Input.mousePosition.x, Input.mousePosition.y);
			RotationButton.OnHeld();
			return false;
		};
		RotationButton.Selectable.OnInteractEnded += () => {
			_isRotating = false;
			RotationButton.OnRelease();
		};
	}

	protected void Update() {
		if (_status == STATUS.CREATED) {
			Timer.Value += Time.deltaTime / SECONDS_BEFORE_ACTIVATION;
			StageDisplay.Progress = Timer.Value;
			StageDisplay.ResetProgress = Timer.Value;
		} else if (_status == STATUS.STARTED || (_status == STATUS.GENERATED && _passedStagesCount >= 3)) {
			_solvingTimer += Time.deltaTime;
			float timer = _solvingTimer / TIME_TO_SOLVE;
			if (timer > 1) {
				CustomLogger("Time's up. Strike.");
				StrikeAndReset();
			} else Timer.Value = timer;
		}
		if (_isRotating) {
			Vector2 newMousePosition = new Vector2(Input.mousePosition.x, Input.mousePosition.y);
			Vector2 dv2 = newMousePosition - _mouseRotationPosition;
			_mouseRotationPosition = newMousePosition;
			OctominoRenderer2D.Rotate(dv2);
			OctominoRenderer3D.Rotate(dv2);
			OctominoRenderer4D.Rotate(dv2);
		}
		if (_isResetting) {
			if (_status == STATUS.GENERATED && _passedStagesCount < 3) {
				float progress = (Time.time - _resettingTime) / RESETTING_TIME;
				if (progress < 1f) StageDisplay.ResetProgress = progress;
				else {
					CustomLogger("Manual Restart.");
					_isResetting = false;
					StageDisplay.ResetProgress = 0f;
					_passedStagesCount = 0;
					_solvingTimer = 0f;
					_timeCheckPoint = 0f;
					StageDisplay.Progress = 0;
					GeneratePuzzle(STAGES_DIMS[0]);
					Reset();
					ResetOffsetTypesQueue();
					Audio.PlaySoundAtTransform(RESTART_SOUND, transform);
				}
			} else {
				_isResetting = false;
				StageDisplay.ResetProgress = 0;
			}
		}
	}

	protected override void OnActivate() {
		base.OnActivate();
		StageDisplay.Progress = 0;
		StageDisplay.ResetProgress = 0;
		Reset();
		StageDisplay.Selectable.OnInteract += () => {
			_isResetting = true;
			_resettingTime = Time.time;
			return false;
		};
		StageDisplay.Selectable.OnInteractEnded += () => {
			_isResetting = false;
			StageDisplay.ResetProgress = 0f;
		};
	}

	private void OnStagePassed() {
		_passedStagesCount += 1;
		CustomLogger("Stage #{0} completed", _passedStagesCount);
		StageDisplay.Progress = (float)_passedStagesCount / STAGES_COUNT;
		if (_passedStagesCount < STAGES_COUNT) {
			_solvingTimer /= 2f;
			CustomLogger("Next stage starting time: {0}s", _solvingTimer.ToString("#.###"));
			if (_passedStagesCount < 3) _timeCheckPoint = _solvingTimer;
			GeneratePuzzle(STAGES_DIMS[_passedStagesCount]);
			Reset();
			Audio.PlaySoundAtTransform(CORRECT_SOUND, transform);
		} else {
			CustomLogger("Module solved");
			Solve();
			Audio.PlayGameSoundAtTransform(KMSoundOverride.SoundEffect.CorrectChime, transform);
			_status = STATUS.SOLVED;
		}
	}

	private void Reset() {
		_isVisited = new bool[OctominoUtils.SIZE];
		_buttonToFaceIndex = Enumerable.Range(0, OctominoUtils.SIZE).ToArray();
		_faceToButtonIndex = new int[OctominoUtils.SIZE];
		_buttonToFaceIndex.Shuffle();
		for (int i = 0; i < OctominoUtils.SIZE; i++) {
			_faceToButtonIndex[_buttonToFaceIndex[i]] = i;
			ButtonComponent button = _buttons[i];
			button.Color = _colors[_buttonToFaceIndex[i]];
			button.Renderer.enabled = false;
			button.BorderSize = ButtonComponent.DEFAULT_BORDER_SIZE;
		}
		_finishButtonIndex = Random.Range(0, OctominoUtils.SIZE);
		CustomLogger("Finish button: #{0}({1})", _finishButtonIndex + 1, ColorToSymbol(_colors[_buttonToFaceIndex[_finishButtonIndex]]));
		if (_passedStagesCount == 0) _offset0 = OFFSET_TYPE.SOLVES_COUNT;
		else if (_passedStagesCount == 1) _offset0 = OFFSET_TYPE.STAGE_NUMBER;
		else _offset0 = GetOffsetType(null);
		_offset1 = _passedStagesCount < 3 ? OFFSET_TYPE.NO_OFFSET : GetOffsetType(new [] { _offset0 });
		_offset2 = _passedStagesCount < 4 ? OFFSET_TYPE.NO_OFFSET : GetOffsetType(new [] { _offset0, _offset1 });
		StageDisplay.OuterColor_0 = OFFSET_TYPES_COLORS[(int)_offset0];
		StageDisplay.OuterColor_1 = OFFSET_TYPES_COLORS[(int)_offset1];
		StageDisplay.OuterColor_2 = OFFSET_TYPES_COLORS[(int)_offset2];
		StageDisplay.OuterColorsCount = _offset2 != OFFSET_TYPE.NO_OFFSET ? 3 : _offset1 != OFFSET_TYPE.NO_OFFSET ? 2 : 1;
		LogOffsets();
		_buttons[_finishButtonIndex].Renderer.enabled = true;
		_status = STATUS.GENERATED;
		Timer.Value = _solvingTimer / TIME_TO_SOLVE;
		_path = "";
	}

	private void CreateButtons() {
		_offsetDirection = Random.Range(0, 2) == 0 ? 1 : -1;
		CustomLogger("Offset direction: {0}", _offsetDirection < 0 ? "LEFT" : "RIGHT");
		float firstButtonXOffset = -(OctominoUtils.SIZE - 1) * BUTTONS_OFFSET;
		float firstButtonYOffset = BUTTONS_OFFSET * (-_offsetDirection);
		List<KMSelectable> selectables = new List<KMSelectable>();
		_buttons = new ButtonComponent[OctominoUtils.SIZE];
		for (int index = 0; index < OctominoUtils.SIZE; index++) {
			int indexClosure = index;
			ButtonComponent btn = Instantiate(ButtonPrefab);
			btn.transform.parent = ButtonsContainer;
			btn.transform.localPosition = new Vector3(firstButtonXOffset + index * 2 * BUTTONS_OFFSET, 0, firstButtonYOffset * (index % 2 == 0 ? 1 : -1));
			btn.transform.localRotation = Quaternion.Euler(0, 45, 0);
			btn.transform.localScale = Vector3.one;
			btn.Color = _colors[index];
			btn.Selectable.Parent = Selectable;
			selectables.Add(btn.Selectable);
			btn.Selectable.OnInteract += () => { OnButtonPressed(indexClosure); return false; };
			_buttons[index] = btn;
		}
		Selectable.Children = Selectable.Children.Concat(selectables).ToArray();
		Selectable.UpdateChildrenProperly();
	}

	private bool OnButtonPressed(int index) {
		int faceIndex = _buttonToFaceIndex[index];
		string faceSymbol = ColorToSymbol(_colors[faceIndex]);
		switch (_status) {
			case STATUS.GENERATED:
				if (new [] { _offset0, _offset1, _offset2 }.Contains(OFFSET_TYPE.STRIKES_COUNT)) CustomLogger("Current strikes count: {0}", BombInfo.GetStrikes());
				if (new [] { _offset0, _offset1, _offset2 }.Contains(OFFSET_TYPE.SOLVES_COUNT)) CustomLogger("Current solves count: {0}", GetOffset(OFFSET_TYPE.SOLVES_COUNT));
				int offsetSum = GetOffset(_offset0) + GetOffset(_offset1) + GetOffset(_offset2);
				int startButtonIndex = TrueModulo(_finishButtonIndex + _offsetDirection * offsetSum, OctominoUtils.SIZE);
				if (startButtonIndex != index) {
					CustomLogger("Expected starting button: {0}. Pressed button: {1}. Strike.", startButtonIndex + 1, index + 1);
					return StrikeAndReset(disableFallback: true);
				}
				for (int i = 0; i < OctominoUtils.SIZE; i++) _buttons[i].Renderer.enabled = true;
				_buttons[index].BorderSize = 1;
				_currentButtonIndex = index;
				_isVisited[faceIndex] = true;
				_status = STATUS.STARTED;
				_path = faceSymbol;
				CustomLogger("Puzzle started from button #{0}({1})", startButtonIndex + 1, _path);
				CustomLogger("Buttons: {0}", Enumerable.Range(0, 8).Select(i => string.Format("#{0}({1})", i + 1, ColorToSymbol(_colors[_buttonToFaceIndex[i]]))).Join("; "));
				Audio.PlaySoundAtTransform(START_SOUND, transform);
				return true;
			case STATUS.STARTED:
				_path += faceSymbol;
				if (index == _currentButtonIndex) {
					LogPathAndTimer();
					if (index != _finishButtonIndex) {
						CustomLogger("Path is closed with an invalid button: #{0}({1}). Strike.", index + 1, faceSymbol);
						return StrikeAndReset();
					}
					int[] notVisited = _isVisited.Select((v, i) => new { v, i }).Where(p => !p.v).Select(p => p.i).ToArray();
					if (notVisited.Length > 0) {
						string info = notVisited.Select(i => string.Format("#{0}({1})", i + 1, ColorToSymbol(_colors[_buttonToFaceIndex[i]]))).Join("; ");
						CustomLogger("Path is closed with unvisited colors: {0}. Strike.", info);
						return StrikeAndReset();
					}
					_buttons[index].BorderSize = ButtonComponent.DEFAULT_BORDER_SIZE;
					OnStagePassed();
					return true;
				}
				bool isAdjacentToCurrentPosition = false;
				int minUnvisitedButtonIndex = int.MaxValue;
				int[] adjacentIndices = _puzzle[_buttonToFaceIndex[_currentButtonIndex]].adjacentIndices;
				for (int i = 0; i < adjacentIndices.Length; i++) {
					int adjacentFace = adjacentIndices[i];
					if (adjacentFace == faceIndex) isAdjacentToCurrentPosition = true;
					if (!_isVisited[adjacentFace]) minUnvisitedButtonIndex = Mathf.Min(minUnvisitedButtonIndex, _faceToButtonIndex[adjacentFace]);
				}
				if (!isAdjacentToCurrentPosition) {
					LogPathAndTimer();
					CustomLogger("Pressed the button not adjacent to the current location. Strike.");
					return StrikeAndReset();
				}
				if (minUnvisitedButtonIndex != int.MaxValue && minUnvisitedButtonIndex != index) {
					LogPathAndTimer();
					string expectedFaceSymbol = ColorToSymbol(_colors[_buttonToFaceIndex[minUnvisitedButtonIndex]]);
					CustomLogger("Moved to #{0}({1}) while moving to #{2}({3}) is expected. Strike.", index + 1, faceSymbol, minUnvisitedButtonIndex + 1, expectedFaceSymbol);
					return StrikeAndReset();
				}
				_buttons[_currentButtonIndex].BorderSize = ButtonComponent.DEFAULT_BORDER_SIZE;
				_buttons[index].BorderSize = 1f;
				_currentButtonIndex = index;
				_isVisited[faceIndex] = true;
				Audio.PlaySoundAtTransform(MOVE_SOUND, transform);
				return true;
			default:
				return false;
		}
	}

	private bool StrikeAndReset(bool disableFallback = false) {
		Strike();
		if (_passedStagesCount >= 3) {
			if (!disableFallback) {
				_solvingTimer = _timeCheckPoint;
				_passedStagesCount = 2;
				CustomLogger("Falling back to stage #3. Starting time: {0}s", _solvingTimer.ToString("#.###"));
				StageDisplay.Progress = (float)_passedStagesCount / STAGES_COUNT;
				GeneratePuzzle(STAGES_DIMS[_passedStagesCount]);
			}
		} else _solvingTimer = _timeCheckPoint;
		Reset();
		return true;
	}

	private void GeneratePuzzle(int dims) {
		// int maxDistance = -1;
		// int maxDistance = 6 - dims;
		int maxDistance = dims == 2 ? 4 : 3;
		_puzzle = OctominoUtils.GeneratePuzzle(dims, OctominoUtils.SIZE, maxDistance);
		// 3 - dim index of 4th dimension (counting from 0)
		if (dims == 4) _puzzle = OctominoUtils.RotateToMaximizeDimensionWidth(dims, _puzzle, 3);
		_colors.Shuffle();
		CustomLogger("Stage #{0}. Generated puzzle ({1}D):", _passedStagesCount + 1, dims);
		for (int i = 0; i < _puzzle.Length; i++) {
			string adjacents = _puzzle[i].adjacentIndices.Select(j => ColorToSymbol(_colors[j])).Join("");
			CustomLogger("{0}: coord={{{1}}}; adjacent={2}", ColorToSymbol(_colors[i]), _puzzle[i].position.Value.Join(","), adjacents);
		}
		ClearAndDisableOctominoRenderers();
		IOctominoRenderer renderer = GetOctominoRenderer(dims);
		renderer.enabled = true;
		renderer.Initialize(_puzzle, _colors);
	}

	private IOctominoRenderer GetOctominoRenderer(int dims) {
		switch (dims) {
			case 2: return OctominoRenderer2D;
			case 3: return OctominoRenderer3D;
			case 4: return OctominoRenderer4D;
			default: throw new System.NotImplementedException();
		}
	}

	private void ClearAndDisableOctominoRenderers() {
		OctominoRenderer2D.Clear();
		OctominoRenderer3D.Clear();
		OctominoRenderer4D.Clear();
		OctominoRenderer2D.enabled = false;
		OctominoRenderer3D.enabled = false;
		OctominoRenderer4D.enabled = false;
	}

	private void ResetOffsetTypesQueue() {
		List<OFFSET_TYPE> list = new List<OFFSET_TYPE>(ALL_OFFSET_TYPES);
		list.Remove(OFFSET_TYPE.SOLVES_COUNT);
		list.Remove(OFFSET_TYPE.STAGE_NUMBER);
		list.Shuffle();
		_offsetTypesQueue = new Queue<OFFSET_TYPE>(list);
	}

	private OFFSET_TYPE GetOffsetType(IEnumerable<OFFSET_TYPE> ignore = null) {
		// TODO: rewrite without using do-while-loop
		OFFSET_TYPE result;
		do {
			if (_offsetTypesQueue.Count > 0) result = _offsetTypesQueue.Dequeue();
			else result = ALL_OFFSET_TYPES.PickRandom();
		} while (ignore != null && ignore.Contains(result));
		return result;
	}

	private int GetOffset(OFFSET_TYPE type) {
		switch (type) {
			case OFFSET_TYPE.NO_OFFSET: return 0;
			case OFFSET_TYPE.STRIKES_COUNT: return BombInfo.GetStrikes();
			case OFFSET_TYPE.SOLVES_COUNT: return BombInfo.GetSolvedModuleIDs().Count;
			case OFFSET_TYPE.PORTS_COUNT: return BombInfo.GetPortCount();
			case OFFSET_TYPE.INDICATORS_COUNT: return BombInfo.GetIndicators().Count();
			case OFFSET_TYPE.BATTERIES_COUNT: return BombInfo.GetBatteryCount();
			case OFFSET_TYPE.STAGE_NUMBER: return _passedStagesCount + 1;
			case OFFSET_TYPE.LAST_SN_DIGIT: return BombInfo.GetSerialNumberNumbers().LastOrDefault();
			case OFFSET_TYPE.MODULES_COUNT: return BombInfo.GetModuleIDs().Count();
			default: throw new System.NotImplementedException();
		}
	}

	protected void CustomLogger(string str, params object[] args) {
#if UNITY_EDITOR
		Log(str, args);
#else
		Log(EncryptLog(string.Format(str, args)));
#endif
	}

	private void LogPathAndTimer() {
		CustomLogger("Path: {0}. Timer: {1}s", _path, _solvingTimer.ToString("#.###"));
	}

	private void LogOffsets() {
		if (_offset2 != OFFSET_TYPE.NO_OFFSET) {
			int off1 = LogOffsetCalculation(_offset0, "Offset 1/3: ");
			int off2 = LogOffsetCalculation(_offset1, "Offset 2/3: ");
			int off3 = LogOffsetCalculation(_offset2, "Offset 3/3: ");
			CustomLogger("Total offset: {0}+{1}+{2} = {3}", off1, off2, off3, off1 + off2 + off3);
		} else if (_offset1 != OFFSET_TYPE.NO_OFFSET) {
			int off1 = LogOffsetCalculation(_offset0, "Offset 1/2: ");
			int off2 = LogOffsetCalculation(_offset1, "Offset 2/2: ");
			CustomLogger("Total offset: {0}+{1} = {2}", off1, off2, off1 + off2);
		} else if (_offset0 != OFFSET_TYPE.NO_OFFSET) LogOffsetCalculation(_offset0, "Offset: ");
		else CustomLogger("No offset");
	}

	private int LogOffsetCalculation(OFFSET_TYPE offset, string prefix = "") {
		int result = GetOffset(offset);
		CustomLogger("{0}{1} ({2}) = {3}", prefix, ColorToSymbol(OFFSET_TYPES_COLORS[(int)offset]), GetOffsetDescriptor(offset), result);
		return result;
	}

	private string GetOffsetDescriptor(OFFSET_TYPE offset) {
		switch (offset) {
			case OFFSET_TYPE.NO_OFFSET: return "No offset";
			case OFFSET_TYPE.STRIKES_COUNT: return "Strikes count";
			case OFFSET_TYPE.SOLVES_COUNT: return "Solves count";
			case OFFSET_TYPE.PORTS_COUNT: return "Ports count";
			case OFFSET_TYPE.INDICATORS_COUNT: return "Indicators count";
			case OFFSET_TYPE.BATTERIES_COUNT: return "Batteries count";
			case OFFSET_TYPE.STAGE_NUMBER: return "Stage number";
			case OFFSET_TYPE.LAST_SN_DIGIT: return "Last SN digit";
			case OFFSET_TYPE.MODULES_COUNT: return "Modules count";
			default: throw new System.NotImplementedException();
		}
	}

	private static int TrueModulo(int a, int b) {
		return ((a % b) + b) % b;
	}

	private static string ColorToSymbol(Color clr) {
		string result = string.Format("#{0}", ColorUtility.ToHtmlStringRGB(clr));
		switch (result) {
			case "#FF0000": return "R";
			case "#00FF00": return "G";
			case "#0000FF": return "B";
			case "#00FFFF": return "C";
			case "#FF00FF": return "M";
			case "#FFEB04": return "Y";
			case "#FFFFFF": return "W";
			case "#7F00FF": return "P";
			default: return result;
		}
	}

	// #.octomino-logger
	private static readonly byte[] LOGGER_KEY_A = new byte[] {
		0xc2, 0x9f, 0x1e, 0xbd, 0xce, 0xa5, 0xff, 0x58, 0x8f, 0x79, 0xd0, 0xca, 0xcd, 0xf7, 0x88, 0x52,
		0x27, 0x9a, 0x0f, 0x5e, 0x38, 0xfc, 0xa8, 0x41, 0x07, 0xd5, 0xff, 0xf5, 0x9f, 0x3d, 0x7b, 0x60,
	};

	private const int LOGGER_KEY_B_SIZE = 31;
	private static readonly int LOGGER_OFFSET_LOOP = LOGGER_KEY_A.Length * LOGGER_KEY_B_SIZE;

	private byte[] _loggerKeyB = null;
	private int _loggerKeyOffset = 0;

	private string EncryptLog(string content) {
		if (_loggerKeyB == null) {
			_loggerKeyB = new byte[LOGGER_KEY_B_SIZE];
			RandomNumberGenerator.Create().GetBytes(_loggerKeyB);
			Log(System.Convert.ToBase64String(_loggerKeyB).TrimEnd('='));
		}
		byte[] data = Encoding.UTF8.GetBytes(content);
		for (int i = 0; i < data.Length; i++) {
			int offset = _loggerKeyOffset + i;
			data[i] = (byte)(data[i] ^ LOGGER_KEY_A[offset % LOGGER_KEY_A.Length] ^ _loggerKeyB[offset % LOGGER_KEY_B_SIZE]);
		}
		_loggerKeyOffset = (_loggerKeyOffset + data.Length) % LOGGER_OFFSET_LOOP;
		return System.Convert.ToBase64String(data).TrimEnd('=');
	}
}
