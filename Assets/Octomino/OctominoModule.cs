using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using wawa.Modules;

public class OctominoModule : ModdedModule {

	private enum STATUS {
		CREATED = 0,
		GENERATED = 1,
		STARTED = 2,
		SOLVED = 3,
	}

	public Transform FacesContainer;
	public Transform ButtonsContainer;
	public OctominoTimer Timer;

	// self components
	public KMSelectable Selectable;
	public KMAudio Audio;

	public FaceComponent FacePrefab;
	public ButtonComponent ButtonPrefab;

	private const float BUTTONS_OFFSET = .006f;
	private const float TIME_TO_SOLVE = 40; // seconds
	private const float SECONDS_BEFORE_ACTIVATION = 6f; // module's timer animation duration before bomb activation

	// sounds
	private const string START_SOUND = "OctominoStart";
	private const string MOVE_SOUND = "OctominoMove";

	private int _startButtonIndex;
	private int _currentButtonIndex;
	private STATUS _status = STATUS.CREATED;
	private float _startedAt;
	private bool[] _isVisited;
	private int[] _buttonToFaceIndex;
	private int[] _faceToButtonIndex;
	private Color[] _colors;
	private OctominoUtils.FaceProps[] _puzzle;
	private ButtonComponent[] _buttons;

	protected void Start() {
		// float colorsOffset = Random.value;
		// float colorsWidth = 1f / OctominoUtils.SIZE;
		// _colors = Enumerable.Range(0, OctominoUtils.SIZE).Select(i => Color.HSVToRGB((colorsOffset + i * colorsWidth) % 1f, 1f, 1f)).ToArray();
		_colors = new Color[] { Color.red, Color.green, Color.blue, Color.cyan, Color.magenta, Color.yellow, Color.white, new Color(.4f, 0, 1f) };
		_colors.Shuffle();
		_puzzle = OctominoUtils.GeneratePuzzle();
		CreateFaces();
		CreateButtons();
		Timer.Value = 1f;
		_startedAt = Time.time;
	}

	protected void Update() {
		switch (_status) {
			case STATUS.CREATED:
				Timer.Value = (Time.time - _startedAt) / SECONDS_BEFORE_ACTIVATION;
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
		for (int index = 0; index < OctominoUtils.SIZE; index++) {
			Log("{0}: ({1},{2}) adj={3}", index, _puzzle[index].position.x, _puzzle[index].position.y, _puzzle[index].adjacentIndices.Join(","));
		}
		Reset();
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

	private void CreateFaces() {
		float containerScale;
		Vector2 facesOffset;
		OctominoUtils.CalculatePuzzleOffsetAndScale(_puzzle, out containerScale, out facesOffset);
		FacesContainer.localScale = Vector3.one * containerScale;
		foreach (OctominoUtils.FaceProps props in _puzzle) {
			FaceComponent face = Instantiate(FacePrefab);
			face.transform.parent = FacesContainer;
			face.transform.localPosition = new Vector3(props.position.x, 0, props.position.y) - new Vector3(facesOffset.x, 0, facesOffset.y);
			face.transform.localRotation = Quaternion.identity;
			face.transform.localScale = Vector3.one;
			face.Color = _colors[props.index];
		}
		FacesContainer.localRotation = Quaternion.Euler(0, 45f + Random.Range(0, 4) * 90f, 0);
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
			// btn.Renderer.enabled = false;
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
						Solve();
						Audio.PlayGameSoundAtTransform(KMSoundOverride.SoundEffect.CorrectChime, transform);
						_status = STATUS.SOLVED;
						_buttons[index].BorderSize = ButtonComponent.DEFAULT_BORDER_SIZE;
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
}
