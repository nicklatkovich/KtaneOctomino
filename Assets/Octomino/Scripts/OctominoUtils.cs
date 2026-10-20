using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using UnityEngine;
using UnityEngine.Assertions;
using MultiDimVectorInt = MultiDimVector<int>;
using MultiDimVectorFloat = MultiDimVector<float>;

using Array = System.Array;

public static class OctominoUtils {
	public const int SIZE = 8;

	public struct FaceProps {
		public readonly int index;
		public readonly MultiDimVectorInt position;
		public readonly int[] adjacentIndices;
		public FaceProps(int index, MultiDimVectorInt position, int[] adjacentIndices) {
			this.index = index;
			this.position = position;
			this.adjacentIndices = adjacentIndices.ToArray();
		}
	}

	// "DD" - array of adjacent cells to zero coordinate
	public static readonly ReadOnlyCollection<MultiDimVectorInt> DD2 = GenerateDD(2);
	public static readonly ReadOnlyCollection<MultiDimVectorInt> DD3 = GenerateDD(3);
	public static readonly ReadOnlyCollection<MultiDimVectorInt> DD4 = GenerateDD(4);

	// cache used DDs
	public static ReadOnlyCollection<MultiDimVectorInt> GetDD(int dims) {
		switch (dims) {
			case 2: return DD2;
			case 3: return DD3;
			case 4: return DD4;
			default: return GenerateDD(dims);
		}
	}

	// cache used N-cube points
	public static readonly ReadOnlyCollection<MultiDimVectorFloat> SQUARE_POINTS = GenerateNCubePoints(2);
	public static readonly ReadOnlyCollection<MultiDimVectorFloat> CUBE_POINTS = GenerateNCubePoints(3);
	public static readonly ReadOnlyCollection<MultiDimVectorFloat> TESSERACT_POINTS = GenerateNCubePoints(4);

	public static ReadOnlyCollection<MultiDimVectorFloat> GetNCubePoints(int dims) {
		switch (dims) {
			case 2: return SQUARE_POINTS;
			case 3: return CUBE_POINTS;
			case 4: return TESSERACT_POINTS;
			default: return GenerateNCubePoints(dims);
		}
	}

	public static FaceProps[] GeneratePuzzle(int dims, int size = SIZE, int maxWidth = -1) {
		MultiDimVectorInt[] facesPositions = new MultiDimVectorInt[size];
		List<int>[] facesAdjacentIndices = new List<int>[size];
		for (int i = 0; i < size; i++) facesAdjacentIndices[i] = new List<int>();
		Dictionary<MultiDimVectorInt, int> positionToIndexMap = new Dictionary<MultiDimVectorInt, int>();
		List<MultiDimVectorInt> queue = new List<MultiDimVectorInt> { new MultiDimVectorInt(dims) };
		MultiDimVectorInt min = new MultiDimVectorInt(dims, int.MaxValue);
		MultiDimVectorInt max = new MultiDimVectorInt(dims, int.MinValue);
		int generatedFacesCount = 0;
		while (generatedFacesCount < size) {
			MultiDimVectorInt pos = queue.PickRandom();
			queue.Remove(pos);
			if (positionToIndexMap.ContainsKey(pos) && positionToIndexMap[pos] >= 0) continue;
			if (maxWidth > 0) {
				if (Enumerable.Range(0, dims).Any(dim => pos[dim] - min[dim] >= maxWidth) || Enumerable.Range(0, dims).Any(dim => max[dim] - pos[dim] >= maxWidth)) {
					continue;
				}
			}
			int index = generatedFacesCount++;
			foreach (MultiDimVectorInt dd in GetDD(dims)) {
				MultiDimVectorInt adj = pos.Add(dd);
				queue.Add(adj); // queue.Add is outside of if block to make stretched octominos less common
				if (!positionToIndexMap.ContainsKey(adj)) {
					positionToIndexMap[adj] = -1;
					continue;
				}
				int adjIndex = positionToIndexMap[adj];
				if (adjIndex < 0) continue; // position is in queue
				facesAdjacentIndices[adjIndex].Add(index);
				facesAdjacentIndices[index].Add(adjIndex);
			}
			min = new MultiDimVectorInt(Enumerable.Range(0, dims).Select(dim => Mathf.Min(pos[dim], min[dim])));
			max = new MultiDimVectorInt(Enumerable.Range(0, dims).Select(dim => Mathf.Max(pos[dim], max[dim])));
			facesPositions[index] = pos;
			positionToIndexMap[pos] = index;
		}
		MultiDimVectorInt positionOffset = new MultiDimVectorInt(Enumerable.Range(0, dims).Select(i => facesPositions.Min(p => p[i])).ToArray());
		return Enumerable.Range(0, size).Select(index => new FaceProps(index, facesPositions[index].Add(positionOffset), facesAdjacentIndices[index].ToArray())).ToArray();
	}

	// Swaps dimensions to make puzzle more diverse.
	// Module uses this function only for 4D-Octomino to feature 4th dimension.
	public static FaceProps[] RotateToMaximizeDimensionWidth(int dims, FaceProps[] puzzle, int dimensionIndex) {
		// for (int i = 0; i < puzzle.Length; i++) Debug.LogFormat("{0}: {1}", i, puzzle[i].position.Value.Join(","));
		int maxWidth = 0;
		List<int> maxWidthDimensionsList = new List<int>();
		for (int dim = 0; dim < dims; dim++) {
			int min = int.MaxValue;
			int max = int.MinValue;
			foreach (FaceProps face in puzzle) {
				min = Mathf.Min(min, face.position[dim]);
				max = Mathf.Max(max, face.position[dim]);
			}
			int width = max - min + 1;
			Debug.LogFormat("{0}th dimension minmax: [{1};{2}] (width={3})", dim, min, max, width);
			if (width > maxWidth) {
				maxWidth = width;
				maxWidthDimensionsList = new List<int>() { dim };
			} else if (width == maxWidth) maxWidthDimensionsList.Add(dim);
		}
		Debug.LogFormat("maxWidth={0}. Dims: ({1})", maxWidth, maxWidthDimensionsList.Join(","));
		Assert.IsTrue(maxWidth > 0, "Failed to calculate maxWidth for puzzle");
		int[] maxWidthDimensions = maxWidthDimensionsList.ToArray();
		int maxScore = -1;
		List<int> maxScoreDimensions = new List<int>();
		for (int i = 0; i < maxWidthDimensions.Length; i++) {
			int dim = maxWidthDimensions[i];
			Dictionary<int, int> cellsCount = new Dictionary<int, int>();
			foreach (FaceProps face in puzzle) {
				int pos = face.position[dim];
				if (!cellsCount.ContainsKey(pos)) cellsCount[pos] = 1;
				else cellsCount[pos] += 1;
			}
			// score calculates diversity by multiplying cells count for each dimension's position,
			// e.g. [3,1] is less diverse than [2,2] while having the same number of cells
			int score = 1;
			foreach (int count in cellsCount.Values) score *= count;
			Debug.LogFormat("{0}th dim score:{1} ({2})", dim, score, cellsCount.Values.Join(","));
			if (score > maxScore) {
				maxScore = score;
				maxScoreDimensions = new List<int>() { dim };
			} else if (score == maxScore) maxScoreDimensions.Add(dim);
		}
		int maxScoreDim = maxScoreDimensions.PickRandom();
		if (maxScoreDim == dimensionIndex) return puzzle; // is best dimension already
		return puzzle.Select(face => {
			int[] pos = face.position.Value.ToArray();
			// swap dimensions
			int tmp = pos[dimensionIndex];
			pos[dimensionIndex] = pos[maxScoreDim];
			pos[maxScoreDim] = tmp;
			return new FaceProps(face.index, new MultiDimVectorInt(pos), face.adjacentIndices);
		}).ToArray();
	}

	public static void CalculatePuzzleOffsetAndScale(int dims, FaceProps[] puzzle, out float scale, out MultiDimVectorFloat offset) {
		// TODO: rewrite using bounding N-sphere algorithm
		float _scale;
		MultiDimVectorFloat _offset;
		CalculatePuzzleOffsetAndScaleUsingMaxDiameter(dims, puzzle, out _scale, out _offset);
		float radius = .5f / _scale;
		bool isValid = true;
		for (int i = 0; i < puzzle.Length; i++) {
			MultiDimVectorFloat pos = puzzle[i].position.ToFloat();
			if (GetNCubePoints(dims).Any(off => MultiDimVectorExtensions.Distance(pos.Add(off), _offset) > radius + Mathf.Epsilon)) {
				isValid = false;
				break;
			}
		}
		if (isValid) {
			scale = _scale;
			offset = _offset;
			return;
		}
		CalculatePuzzleOffsetAndScaleUsingRectangle(dims, puzzle, out scale, out offset);
	}

	private static void CalculatePuzzleOffsetAndScaleUsingRectangle(int dims, FaceProps[] puzzle, out float scale, out MultiDimVectorFloat offset) {
		MultiDimVectorInt minNCubeCenter = new MultiDimVectorInt(Enumerable.Range(0, dims).Select(dim => Mathf.Min(puzzle.Select(f => f.position[dim]).ToArray())));
		MultiDimVectorInt maxNCubeCenter = new MultiDimVectorInt(Enumerable.Range(0, dims).Select(dim => Mathf.Max(puzzle.Select(f => f.position[dim]).ToArray())));
		MultiDimVectorFloat min = minNCubeCenter.ToFloat().Sub(new MultiDimVectorFloat(dims, .5f));
		MultiDimVectorFloat max = maxNCubeCenter.ToFloat().Add(new MultiDimVectorFloat(dims, .5f));
		float dist = MultiDimVectorExtensions.Distance(min, max);
		scale = 1f / dist;
		offset = min.Add(max).Div(2f);
	}

	private static void CalculatePuzzleOffsetAndScaleUsingMaxDiameter(int dims, FaceProps[] puzzle, out float scale, out MultiDimVectorFloat offset) {
		offset = new MultiDimVectorFloat();
		float maxDiameter = 0;
		for (int i = 0; i < puzzle.Length; i++) {
			MultiDimVectorInt pos = puzzle[i].position;
			for (int j = i + 1; j < puzzle.Length; j++) {
				MultiDimVectorInt posJ = puzzle[j].position;
				MultiDimVectorFloat min = new MultiDimVectorFloat(Enumerable.Range(0, dims).Select(k => Mathf.Min(pos[k], posJ[k]) - .5f));
				MultiDimVectorFloat max = new MultiDimVectorFloat(Enumerable.Range(0, dims).Select(k => Mathf.Max(pos[k], posJ[k]) + .5f));
				float distance = MultiDimVectorExtensions.Distance(min, max);
				if (distance > maxDiameter) {
					maxDiameter = distance;
					offset = min.Add(max).Div(2f);
				}
			}
		}
		Assert.IsTrue(maxDiameter > 0, "Octomino: Failed to get puzzle scale");
		scale = 1f / maxDiameter;
	}

	public static ReadOnlyCollection<MultiDimVectorInt> GenerateDD(int dims) {
		List<MultiDimVectorInt> res = new List<MultiDimVectorInt>();
		int[] arr = new int[dims];
		for (int i = 0; i < dims; i++) {
			arr[i] = 1;
			res.Add(new MultiDimVectorInt(arr));
			arr[i] = -1;
			res.Add(new MultiDimVectorInt(arr));
			arr[i] = 0;
		}
		return Array.AsReadOnly(res.ToArray());
	}

	public static ReadOnlyCollection<MultiDimVectorInt> GenerateDDiag(int dims) {
		int maxBinV = 1 << dims;
		List<MultiDimVectorInt> result = new List<MultiDimVectorInt>();
		for (int binV = 0; binV < maxBinV; binV++) {
			List<int> position = new List<int>();
			int v = binV;
			for (int dim = 0; dim < dims; dim++) {
				position.Add((v & 1) > 0 ? 1 : -1);
				v >>= 1;
			}
			result.Add(new MultiDimVectorInt(position));
		}
		return Array.AsReadOnly(result.ToArray());
	}

	public static ReadOnlyCollection<MultiDimVectorFloat> GenerateNCubePoints(int dims) {
		return Array.AsReadOnly(GenerateDDiag(dims).Select(vec => vec.ToFloat().Div(2f)).ToArray());
	}
}
