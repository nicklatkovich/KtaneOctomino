using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using UnityEngine;
using UnityEngine.Assertions;

public struct MultiDimVector<T> {

	public readonly ReadOnlyCollection<T> Value;

	public int Dims { get { return Value.Count; } }
	public T this[int i] { get { return Value[i]; } }

	public MultiDimVector(T[] value) { Value = Array.AsReadOnly(value.ToArray()); }
	public MultiDimVector(IEnumerable<T> value) : this(value.ToArray()) { }
	public MultiDimVector(int dims) : this(new T[dims]) { }
	public MultiDimVector(int dims, T def) : this(Enumerable.Repeat(def, dims)) { }

	public static MultiDimVector<T> Add(MultiDimVector<T> v1, MultiDimVector<T> v2, Func<T, T, T> add) {
		Assert.AreEqual(v1.Dims, v2.Dims);
		return new MultiDimVector<T>(Enumerable.Range(0, v1.Dims).Select(i => add(v1[i], v2[i])).ToArray());
	}

	public static MultiDimVector<int> Add(MultiDimVector<int> vec1, MultiDimVector<int> vec2) { return MultiDimVector<int>.Add(vec1, vec2, (v1, v2) => v1 + v2); }
	public static MultiDimVector<float> Add(MultiDimVector<float> vec1, MultiDimVector<float> vec2) { return MultiDimVector<float>.Add(vec1, vec2, (v1, v2) => v1 + v2); }

	public MultiDimVector<T> Add(MultiDimVector<T> other, Func<T, T, T> add) { return Add(this, other, add); }

	public override bool Equals(object obj) {
		if (!(obj is MultiDimVector<T>)) return false;
		MultiDimVector<T> v = (MultiDimVector<T>)obj;
		ReadOnlyCollection<T> value = Value;
		return Dims == v.Dims && Enumerable.Range(0, Dims).All(i => EqualityComparer<T>.Default.Equals(value[i], v.Value[i]));
	}

	public override int GetHashCode() { return Value.Aggregate(17, (current, item) => (current * 23) + item.GetHashCode()); }
}

public static class MultiDimVectorExtensions {
	public static MultiDimVector<int> Add(this MultiDimVector<int> vec1, MultiDimVector<int> vec2) { return MultiDimVector<int>.Add(vec1, vec2, (a, b) => a + b); }
	public static MultiDimVector<int> Neg(this MultiDimVector<int> vec) { return new MultiDimVector<int>(vec.Value.Select(v => -v).ToArray()); }
	public static MultiDimVector<int> Sub(this MultiDimVector<int> vec1, MultiDimVector<int> vec2) { return vec1.Add(vec2.Neg()); }

	public static MultiDimVector<float> ToFloat(this MultiDimVector<int> vec) { return new MultiDimVector<float>(Enumerable.Range(0, vec.Dims).Select(i => (float)vec[i])); }

	public static MultiDimVector<float> Add(this MultiDimVector<float> vec1, MultiDimVector<float> vec2) { return MultiDimVector<float>.Add(vec1, vec2, (a, b) => a + b); }
	public static MultiDimVector<float> Neg(this MultiDimVector<float> vec) { return new MultiDimVector<float>(vec.Value.Select(v => -v).ToArray()); }
	public static MultiDimVector<float> Sub(this MultiDimVector<float> vec1, MultiDimVector<float> vec2) { return vec1.Add(vec2.Neg()); }
	public static MultiDimVector<float> Div(this MultiDimVector<float> vec, float value) { return new MultiDimVector<float>(vec.Value.Select(v => v / value)); }

	public static float Distance(MultiDimVector<float> vec1, MultiDimVector<float> vec2) {
		float sum = 0f;
		Assert.AreEqual(vec1.Dims, vec2.Dims);
		for (int dim = 0; dim < vec1.Dims; dim++) sum += Mathf.Pow(vec1[dim] - vec2[dim], 2f);
		return Mathf.Sqrt(sum);
	}
}
