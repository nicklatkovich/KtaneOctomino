using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Assertions;

public static class OctominoUtils {
	public const int SIZE = 8;

	public struct FaceProps {
		public readonly int index;
		public readonly Vector2Int position;
		public readonly int[] adjacentIndices;
		public FaceProps(int index, Vector2Int position, int[] adjacentIndices) {
			this.index = index;
			this.position = position;
			this.adjacentIndices = adjacentIndices.ToArray();
		}
	}

	public static readonly Vector2Int[] DD = new Vector2Int[] { Vector2Int.right, Vector2Int.up, Vector2Int.left, Vector2Int.down };
	public static readonly Vector2[] SQUARE_POINTS = new Vector2[] { new Vector2(.5f, .5f), new Vector2(.5f, -.5f), new Vector2(-.5f, -.5f), new Vector2(-.5f, .5f) };

	public static FaceProps[] GeneratePuzzle(int size = SIZE) {
		Vector2Int[] facesPositions = new Vector2Int[size];
		List<int>[] facesAdjacentIndices = new List<int>[size];
		for (int i = 0; i < size; i++) facesAdjacentIndices[i] = new List<int>();
		Dictionary<Vector2Int, int> positionToIndexMap = new Dictionary<Vector2Int, int>();
		HashSet<Vector2Int> queue = new HashSet<Vector2Int> { Vector2Int.zero };
		int generatedFacesCount = 0;
		while (generatedFacesCount < size) {
			Vector2Int pos = queue.PickRandom();
			queue.Remove(pos);
			int index = generatedFacesCount++;
			foreach (Vector2Int dd in DD) {
				Vector2Int adj = pos + dd;
				if (!positionToIndexMap.ContainsKey(adj)) {
					queue.Add(adj);
					positionToIndexMap[adj] = -1;
					continue;
				}
				int adjIndex = positionToIndexMap[adj];
				if (adjIndex < 0) continue; // position is in queue
				facesAdjacentIndices[adjIndex].Add(index);
				facesAdjacentIndices[index].Add(adjIndex);
			}
			facesPositions[index] = pos;
			positionToIndexMap[pos] = index;
		}
		Vector2Int positionOffset = new Vector2Int(facesPositions.Min(pos => pos.x), facesPositions.Min(pos => pos.y)) * -1;
		return Enumerable.Range(0, size).Select(index => new FaceProps(index, facesPositions[index] + positionOffset, facesAdjacentIndices[index].ToArray())).ToArray();
	}

	public static void CalculatePuzzleOffsetAndScale(FaceProps[] puzzle, out float scale, out Vector2 offset) {
		// TODO: rewrite using bounding circle algorithm
		float _scale;
		Vector2 _offset;
		CalculatePuzzleOffsetAndScaleUsingMaxDiameter(puzzle, out _scale, out _offset);
		float radius = .5f / _scale;
		bool isValid = true;
		for (int i = 0; i < puzzle.Length; i++) {
			Vector2 pos = new Vector2(puzzle[i].position.x, puzzle[i].position.y);
			if (SQUARE_POINTS.Any(off => Vector2.Distance(pos + off, _offset) > radius)) {
				isValid = false;
				break;
			}
		}
		if (isValid) {
			scale = _scale;
			offset = _offset;
			return;
		}
		CalculatePuzzleOffsetAndScaleUsingRectangle(puzzle, out scale, out offset);
	}

	private static void CalculatePuzzleOffsetAndScaleUsingRectangle(FaceProps[] puzzle, out float scale, out Vector2 offset) {
		Vector2 min = new Vector2(Mathf.Min(puzzle.Select(f => f.position.x).ToArray()), Mathf.Min(puzzle.Select(f => f.position.y).ToArray())) - Vector2.one * .5f;
		Vector2 max = new Vector2(Mathf.Max(puzzle.Select(f => f.position.x).ToArray()), Mathf.Max(puzzle.Select(f => f.position.y).ToArray())) + Vector2.one * .5f;
		float dist = Vector2.Distance(min, max);
		scale = 1f / dist;
		offset = (min + max) / 2f;
	}

	private static void CalculatePuzzleOffsetAndScaleUsingMaxDiameter(FaceProps[] puzzle, out float scale, out Vector2 offset) {
		offset = Vector2.zero;
		float maxDiameter = 0;
		for (int i = 0; i < puzzle.Length; i++) {
			Vector2Int pos = puzzle[i].position;
			for (int j = i + 1; j < puzzle.Length; j++) {
				Vector2Int posJ = puzzle[j].position;
				Vector2 min = new Vector2(Mathf.Min(pos.x, posJ.x) - .5f, Mathf.Min(pos.y, posJ.y) - .5f);
				Vector2 max = new Vector2(Mathf.Max(pos.x, posJ.x) + .5f, Mathf.Max(pos.y, posJ.y) + .5f);
				float distance = Vector2.Distance(min, max);
				if (distance > maxDiameter) {
					maxDiameter = distance;
					offset = (min + max) / 2f;
				}
			}
		}
		Assert.IsTrue(maxDiameter > 0, "Octomino: Failed to get puzzle scale");
		scale = 1f / maxDiameter;
	}
}
