using System.Collections.Generic;
using System.Linq;
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

	// self components
	public KMSelectable Selectable;
	public KMAudio Audio;

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

	private readonly int[] STAGES_DIMS = new int[] { 2, 3, 4, 3, 2 };
	// private readonly int[] STAGES_DIMS = new int[] { 3, 3, 3, 3, 3 };
	// private readonly int[] STAGES_DIMS = new int[] { 4, 4, 4, 4, 4 };

	private readonly Color[] _colors = new Color[] { Color.red, Color.green, Color.blue, Color.cyan, Color.magenta, Color.yellow, Color.white, new Color(.4f, 0, 1f) };

	private Vector2 _mouseRotationPosition;
	private bool _isRotating = false;
	private float _resettingTime;
	private bool _isResetting = false;
	private int _startButtonIndex;
	private int _currentButtonIndex;
	private int _passedStagesCount;
	private STATUS _status = STATUS.CREATED;
	private float _solvingTimer = 0f;
	private float _timeCheckPoint = 0f;
	private bool[] _isVisited;
	private int[] _buttonToFaceIndex;
	private int[] _faceToButtonIndex;
	private OctominoUtils.FaceProps[] _puzzle;
	private ButtonComponent[] _buttons;

	protected void Start() {
		CreateButtons();
		StageDisplay.OuterColor = _colors.PickRandom();
		_colors.Shuffle();
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
			if (_status == STATUS.GENERATED) {
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
		StageDisplay.OuterColor = Color.black;
		Reset();
	}

	private void OnStagePassed() {
		_passedStagesCount += 1;
		if (_passedStagesCount < STAGES_COUNT) {
			_solvingTimer /= 2f;
			if (_passedStagesCount < 3) _timeCheckPoint = _solvingTimer;
			StageDisplay.Progress = (float)_passedStagesCount / STAGES_COUNT;
			GeneratePuzzle(STAGES_DIMS[_passedStagesCount]);
			Reset();
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
		_startButtonIndex = Random.Range(0, OctominoUtils.SIZE);
		_buttons[_startButtonIndex].Renderer.enabled = true;
		_status = STATUS.GENERATED;
		Timer.Value = _solvingTimer / TIME_TO_SOLVE;
	}

	private void CreateButtons() {
		float firstButtonXOffset = -(OctominoUtils.SIZE - 1) * BUTTONS_OFFSET;
		float firstButtonYOffset = BUTTONS_OFFSET * (Random.Range(0, 2) == 0 ? 1 : -1);
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
				if (index != _startButtonIndex) return false;
				for (int i = 0; i < OctominoUtils.SIZE; i++) _buttons[i].Renderer.enabled = true;
				_buttons[index].BorderSize = 1;
				_currentButtonIndex = index;
				_isVisited[_buttonToFaceIndex[index]] = true;
				_status = STATUS.STARTED;
				Audio.PlaySoundAtTransform(START_SOUND, transform);
				return true;
			case STATUS.STARTED:
				if (index == _currentButtonIndex) {
					if (index == _startButtonIndex && _isVisited.All(v => v)) {
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
}
