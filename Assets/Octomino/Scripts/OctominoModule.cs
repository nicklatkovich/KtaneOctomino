using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using wawa.Modules;

public class OctominoModule : ModdedModule {

	public int STAGES_COUNT = 5;

	// assuming each stage takes up to 20 seconds to complete:
	// 1. 20
	// 2. 20/2+20 = 30
	// 3. 30/2+20 = 35
	// 4. 35/2+20 = 37.5
	// 5. 37.5/2+20 = 38.75
	public float TIME_TO_SOLVE = 40; // seconds

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

	public OctominoRenderer2D OctominoRenderer2D;
	public OctominoRenderer3D OctominoRenderer3D;
	public OctominoRenderer4D OctominoRenderer4D;

	private const float BUTTONS_OFFSET = .006f;
	private const float SECONDS_BEFORE_ACTIVATION = 6f; // module's timer animation duration before bomb activation

	// sounds
	private const string START_SOUND = "OctominoStart";
	private const string MOVE_SOUND = "OctominoMove";

	private readonly int[] STAGES_DIMS = new int[] { 2, 3, 4, 3, 2 };
	// private readonly int[] STAGES_DIMS = new int[] { 4, 4, 4, 4, 4 };

	private readonly Color[] _colors = new Color[] { Color.red, Color.green, Color.blue, Color.cyan, Color.magenta, Color.yellow, Color.white, new Color(.4f, 0, 1f) };

	private int _startButtonIndex;
	private int _currentButtonIndex;
	private int _passedStagesCount;
	private STATUS _status = STATUS.CREATED;
	private float _startedAt;
	private bool[] _isVisited;
	private int[] _buttonToFaceIndex;
	private int[] _faceToButtonIndex;
	private OctominoUtils.FaceProps[] _puzzle;
	private ButtonComponent[] _buttons;

	protected void Start() {
		ClearAndDisableOctominoRenderers();
		CreateButtons();
		StageDisplay.OuterColor = _colors.PickRandom();
		_startedAt = Time.time;
		_colors.Shuffle();
		GeneratePuzzle(STAGES_DIMS[0]);
	}

	protected void Update() {
		switch (_status) {
			case STATUS.CREATED:
				float loader = (Time.time - _startedAt) / SECONDS_BEFORE_ACTIVATION;
				Timer.Value = loader;
				StageDisplay.Progress = loader;
				StageDisplay.ResetProgress = loader;
				break;
			case STATUS.STARTED:
				float timer = (Time.time - _startedAt) / TIME_TO_SOLVE;
				if (timer > 1) StrikeAndReset();
				else Timer.Value = timer;
				break;
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
		StageDisplay.Progress = (float)_passedStagesCount / STAGES_COUNT;
		if (_passedStagesCount < STAGES_COUNT) {
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
		Timer.Value = 0f;
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
		Selectable.Children = selectables.ToArray();
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
				_startedAt = Time.time;
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
		Reset();
		return true;
	}

	private void GeneratePuzzle(int dims) {
		// int maxDistance = -1;
		// int maxDistance = 6 - dims;
		int maxDistance = dims == 2 ? 4 : 3;
		_puzzle = OctominoUtils.GeneratePuzzle(dims, OctominoUtils.SIZE, maxDistance);
		ClearAndDisableOctominoRenderers();
		IOctominoRenderer renderer = GetOctominoRenderer(dims);
		renderer.Initialize(_puzzle, _colors);
		renderer.enabled = true;
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
