using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace MusicManager.PrsSearch.DataMatching.Util
{
    public class Matrix
    {
        private readonly float[,] _m;
        private readonly int _size;

        public int Size
        {
            get { return _size; }
        }

        public Matrix(int size)
        {
            _m = new float[size, size];
            _size = size;
        }

        public float this[int i, int j]
        {
            get { return _m[i, j]; }
            set { _m[i, j] = value; }
        }

        public IList<int[]> GetAllPermutations()
        {
            /* We need to get every permutation of the y-coordinate (n!). e.g., for size 3:
             * 
             * 0 1 2
             * 0 2 1
             * 1 0 2
             * 1 2 1
             * 2 0 1
             * 2 1 0 */

            return GetAllPermutations(new HashSet<int>(Enumerable.Range(0, _size)),
                                      new int[_size]);
        }

        private IList<int[]> GetAllPermutations(HashSet<int> available, int[] permutation, int position = 0)
        {
            var result = new List<int[]>();

            for (int i = 0; i < _size; i++)
            {
                if (!available.Remove(i))
                    continue;

                permutation[position] = i;

                if (!available.Any())
                    result.Add((int[])permutation.Clone());

                result.AddRange(GetAllPermutations(available, permutation, position + 1));

                // put it back
                available.Add(i);
            }

            return result;
        }
    }
}
