using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using KModkit;
using UnityEngine;
using wawa.Modules;

public class OctominoModule : ModdedModule {

	public const int STAGES_COUNT = 5;
	public const float TIME_TO_SOLVE = 90; // seconds
	public const float RESETTING_TIME = 2f;

	private enum STATUS {
		CREATED = 0,
		GENERATED = 1,
		STARTED = 2,
		SOLVED = 3,
	}

	private enum OFFSET_TYPE {
		NO_OFFSET = 0,
		STAGE_NUMBER = 1,
		PORTS_COUNT = 2,
		INDICATORS_COUNT = 3,
		BATTERIES_COUNT = 4,
		STRIKES_COUNT = 5,
	}

	private static readonly ReadOnlyCollection<OFFSET_TYPE> ALL_OFFSET_TYPES = System.Array.AsReadOnly(new OFFSET_TYPE[] {
		OFFSET_TYPE.STAGE_NUMBER,
		OFFSET_TYPE.PORTS_COUNT,
		OFFSET_TYPE.INDICATORS_COUNT,
		OFFSET_TYPE.BATTERIES_COUNT,
		OFFSET_TYPE.STRIKES_COUNT,
	});

	private static readonly ReadOnlyCollection<Color> OFFSET_TYPES_COLORS = System.Array.AsReadOnly(new Color[] {
		Color.black,
		Color.yellow,
		Color.blue,
		Color.cyan,
		Color.magenta,
		Color.red,
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

	private readonly Color[] _colors = new Color[] { Color.red, Color.green, Color.blue, Color.cyan, Color.magenta, Color.yellow, Color.white, new Color(.4f, 0, 1f) };

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
	private bool[] _isVisited;
	private int[] _buttonToFaceIndex;
	private int[] _faceToButtonIndex;
	private OFFSET_TYPE _offset0 = OFFSET_TYPE.NO_OFFSET;
	private OFFSET_TYPE _offset1 = OFFSET_TYPE.NO_OFFSET;
	private Queue<OFFSET_TYPE> _offsetTypesQueue;
	private OctominoUtils.FaceProps[] _puzzle;
	private ButtonComponent[] _buttons;

	protected void Start() {
		CreateButtons();
		StageDisplay.OuterColor_0 = OFFSET_TYPES_COLORS.PickRandom();
		StageDisplay.OuterColor_1 = OFFSET_TYPES_COLORS.PickRandom();
		_colors.Shuffle();
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
			if (timer > 1) StrikeAndReset();
			else Timer.Value = timer;
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
					_isResetting = false;
					StageDisplay.ResetProgress = 0f;
					_passedStagesCount = 0;
					_solvingTimer = 0f;
					_timeCheckPoint = 0f;
					StageDisplay.Progress = 0;
					GeneratePuzzle(STAGES_DIMS[0]);
					Reset();
					_offset0 = OFFSET_TYPE.NO_OFFSET;
					_offset1 = OFFSET_TYPE.NO_OFFSET;
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
		StageDisplay.OuterColor_0 = Color.black;
		StageDisplay.OuterColor_1 = Color.black;
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
		StageDisplay.Progress = (float)_passedStagesCount / STAGES_COUNT;
		if (_passedStagesCount < STAGES_COUNT) {
			_solvingTimer /= 2f;
			if (_passedStagesCount < 3) _timeCheckPoint = _solvingTimer;
			GeneratePuzzle(STAGES_DIMS[_passedStagesCount]);
			Reset();
			Audio.PlaySoundAtTransform(CORRECT_SOUND, transform);
		} else {
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
		if (_passedStagesCount == 0) _offset0 = OFFSET_TYPE.NO_OFFSET;
		else if (_passedStagesCount == 1) _offset0 = OFFSET_TYPE.STAGE_NUMBER;
		else _offset0 = GetOffsetType(null);
		_offset1 = _passedStagesCount < 4 ? OFFSET_TYPE.NO_OFFSET : GetOffsetType(_offset0);
		StageDisplay.OuterColor_0 = OFFSET_TYPES_COLORS[(int)_offset0];
		StageDisplay.OuterColor_1 = OFFSET_TYPES_COLORS[(int)(_offset1 == OFFSET_TYPE.NO_OFFSET ? _offset0 : _offset1)];
		_buttons[_finishButtonIndex].Renderer.enabled = true;
		_status = STATUS.GENERATED;
		Timer.Value = _solvingTimer / TIME_TO_SOLVE;
	}

	private void CreateButtons() {
		_offsetDirection = Random.Range(0, 2) == 0 ? 1 : -1;
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
		switch (_status) {
			case STATUS.GENERATED:
				int startButtonIndex = TrueModulo(_finishButtonIndex + _offsetDirection * (GetOffset(_offset0) + GetOffset(_offset1)), OctominoUtils.SIZE);
				if (startButtonIndex != index) return StrikeAndReset();
				for (int i = 0; i < OctominoUtils.SIZE; i++) _buttons[i].Renderer.enabled = true;
				_buttons[index].BorderSize = 1;
				_currentButtonIndex = index;
				_isVisited[_buttonToFaceIndex[index]] = true;
				_status = STATUS.STARTED;
				Audio.PlaySoundAtTransform(START_SOUND, transform);
				return true;
			case STATUS.STARTED:
				if (index == _currentButtonIndex) {
					if (index == _finishButtonIndex && _isVisited.All(v => v)) {
						_buttons[index].BorderSize = ButtonComponent.DEFAULT_BORDER_SIZE;
						OnStagePassed();
						return true;
					}
					return StrikeAndReset();
				}
				bool isAdjacentToCurrentPosition = false;
				int faceIndex = _buttonToFaceIndex[index];
				int minUnvisitedButtonIndex = int.MaxValue;
				int[] adjacentIndices = _puzzle[_buttonToFaceIndex[_currentButtonIndex]].adjacentIndices;
				for (int i = 0; i < adjacentIndices.Length; i++) {
					int adjacentFace = adjacentIndices[i];
					if (adjacentFace == faceIndex) isAdjacentToCurrentPosition = true;
					if (!_isVisited[adjacentFace]) minUnvisitedButtonIndex = Mathf.Min(minUnvisitedButtonIndex, _faceToButtonIndex[adjacentFace]);
				}
				if (!isAdjacentToCurrentPosition) {
					Log("Pressed button not adjacent to the current location");
					return StrikeAndReset();
				}
				if (minUnvisitedButtonIndex != int.MaxValue && minUnvisitedButtonIndex != index) {
					Log("Moved to visited face while unvisited adjacent exists");
					return StrikeAndReset();
				}
				_buttons[_currentButtonIndex].BorderSize = ButtonComponent.DEFAULT_BORDER_SIZE;
				_buttons[index].BorderSize = 1f;
				_currentButtonIndex = index;
				_isVisited[faceIndex] = true;
				Log("Moved to button #{0} (face {1})", index, faceIndex);
				Audio.PlaySoundAtTransform(MOVE_SOUND, transform);
				return true;
			default:
				return false;
		}
	}

	private bool StrikeAndReset() {
		Strike();
		_solvingTimer = _timeCheckPoint;
		if (_passedStagesCount >= 3) {
			_passedStagesCount = 2;
			GeneratePuzzle(STAGES_DIMS[_passedStagesCount]);
		}
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
		List<OFFSET_TYPE> list = new List<OFFSET_TYPE>() { OFFSET_TYPE.PORTS_COUNT, OFFSET_TYPE.INDICATORS_COUNT, OFFSET_TYPE.BATTERIES_COUNT, OFFSET_TYPE.STRIKES_COUNT };
		list.Shuffle();
		_offsetTypesQueue = new Queue<OFFSET_TYPE>(list);
	}

	private OFFSET_TYPE GetOffsetType(OFFSET_TYPE? ignore) {
		// TODO: rewrite without using do-while-loop
		OFFSET_TYPE result;
		do {
			if (_offsetTypesQueue.Count > 0) result = _offsetTypesQueue.Dequeue();
			else result = ALL_OFFSET_TYPES.PickRandom();
		} while (result == ignore);
		return result;
	}

	private int GetOffset(OFFSET_TYPE type) {
		switch (type) {
			case OFFSET_TYPE.NO_OFFSET: return 0;
			case OFFSET_TYPE.STAGE_NUMBER: return _passedStagesCount + 1;
			case OFFSET_TYPE.PORTS_COUNT: return BombInfo.GetPortCount();
			case OFFSET_TYPE.BATTERIES_COUNT: return BombInfo.GetBatteryCount();
			case OFFSET_TYPE.INDICATORS_COUNT: return BombInfo.GetIndicators().Count();
			case OFFSET_TYPE.STRIKES_COUNT: return BombInfo.GetStrikes();
			default: throw new System.NotImplementedException();
		}
	}

	private static int TrueModulo(int a, int b) {
		return ((a % b) + b) % b;
	}
}
