using System.Numerics;

namespace SeriesCache;

public static class TreeNodeExtensions
{
    public static AVLTree<T, TNumber>.Node FindNearest<T, TNumber>(this AVLTree<T, TNumber> tree, T expecting)
        where TNumber : IBinaryInteger<TNumber>, ISignedNumber<TNumber>, IConvertible
    {
        if (tree.IsEmpty)
            throw new InvalidOperationException("No items to search");
        return tree.Root!.FindNearest(expecting);
    }

    public static AVLTree<T, TNumber>.Node FindNearest<T, TNumber>(this AVLTree<T, TNumber> tree, DistanceWithoutValueReadonly<T, TNumber> distanceFunc)
        where TNumber : IBinaryInteger<TNumber>, ISignedNumber<TNumber>, IConvertible
    {
        if (tree.IsEmpty)
            throw new InvalidOperationException("No items to search");
        return tree.Root!.FindNearest(distanceFunc);
    }

    public static AVLTree<T, TNumber>.Node FindNearest<T, TNumber>(this AVLTree<T, TNumber>.Node root, T expecting)
        where TNumber : IBinaryInteger<TNumber>, ISignedNumber<TNumber>, IConvertible
    {
        DistanceWithoutValueReadonly<T, TNumber> distanceFunc = (ref readonly T item) => root.DistanceFunc!(in expecting, in item);
        return FindNearest(root, distanceFunc);
    }

    public static AVLTree<T, TNumber>.Node FindNearest<T, TNumber>(this AVLTree<T, TNumber>.Node root, DistanceWithoutValueReadonly<T, TNumber> distanceFunc)
        where TNumber : IBinaryInteger<TNumber>, ISignedNumber<TNumber>, IConvertible
    {
        TNumber distance = distanceFunc(in root.RefValue);
        if (distance == TNumber.Zero)
            return root;
        if (distance < TNumber.Zero) 
        {
            if ( root.Left == null)
                return root;
             var leftNearest = root.Left.FindNearest(distanceFunc);
             TNumber leftDistance = TNumber.Abs(distanceFunc(in leftNearest.RefValue));
             if (leftDistance <= TNumber.Abs(distance))
                 return leftNearest;
             return root;
        }
        if (distance > TNumber.Zero)
        {
            if (root.Right == null)
                return root;
             var rightNearest = root.Right.FindNearest(distanceFunc);
             TNumber rightDistance = TNumber.Abs(distanceFunc(in rightNearest.RefValue));
             if (rightDistance < distance)
                 return rightNearest;
             return root;
        }
        throw new InvalidOperationException("Can't happen");
    }
}
