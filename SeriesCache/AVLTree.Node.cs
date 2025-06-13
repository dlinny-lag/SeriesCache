
using System.Collections;
using System.Diagnostics;
using System.Numerics;
using System.Runtime.CompilerServices;

namespace SeriesCache;

public partial class AVLTree<T, TNumber> : IEnumerable<T>, IReadOnlyCollection<T>
    where TNumber : IBinaryInteger<TNumber>, ISignedNumber<TNumber>, IConvertible
{
    [DebuggerDisplay("Node={Value}")]
    public class Node
    {
        private AVLTree<T, TNumber>? owner;
        private T value;
        public Node(T value, AVLTree<T, TNumber> owner, Node? parent)
        {
            this.owner = owner ?? throw new ArgumentNullException(nameof(owner));
            this.value = value;
            Height = 1;
            Parent = parent;
        }

        public DistanceWithValueRef<T, T, TNumber>? DistanceFunc => owner?.DistanceFunc;

        public T Value 
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get { return value; }
        }
        public ref readonly T RefValue 
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get { return ref value; }
        }
        public int Height {get; private set;}

        public Node? Left {get; private set; }
        public Node? Right {get; private set; }
        public Node? Parent {get; private set; }

        /// <summary>
        /// returns new root object, if necessary. not the node that contains <paramref name="item"/>
        /// </summary>
        /// <param name="item"></param>
        /// <returns></returns>
        public Node Add(T item)
        {
            if (owner is null)
                throw new InvalidOperationException("Can't update detached node");
            var compare = owner.DistanceFunc(in item, in value);
            if (compare < TNumber.Zero)
            {
                if (Left is null)
                    Left = new Node(item, owner, this);
                else
                    Left = Left.Add(item);
            }
            else
            { //in case of equal or greater
                if (Right is null) 
                    Right = new Node(item, owner, this);
                else
                    Right = Right.Add(item);
            }
            return Balance();
        }

        internal Node FindMostLeft()
        {
            if (Left is null)
                return this;
            return Left.FindMostLeft();
        }

        internal Node FindMostRight()
        {
            if (Right is null) 
                return this;
            return Right.FindMostRight();
        }

        public void Remove()
        {
            if (owner is null)
                throw new InvalidOperationException("Can't remove detached node");

            var parent = Parent; // backup value
            

            Node? replaceThisByMe = null;

            if (Left is null && Right is null)
            {// I'm a leaf, just remove me
                // nothing to do
            }
            else if (Left is null)
            { // I'm not a leaf, but I have a single leaf, so replace me with leaf. my child is always a leaf if I have a single child
                replaceThisByMe = Right;
            }
            else if (Right is null) 
            { // I'm not a leaf, but I have a single leaf, so replace me with leaf. my child is always a leaf if I have a single child
                replaceThisByMe = Left;
            }
            else
            { // both children are here 
                var mostLeft = Right.FindMostLeft();
                mostLeft.Add(Left.Value);
                replaceThisByMe = Right;
            }

            if (replaceThisByMe is not null)
            {
                replaceThisByMe = replaceThisByMe.Balance();
            }

            // clean up this node
            Parent = null;
            Left = null;
            Right = null;
            
            // update parent
            UpdateInParent(parent, replaceThisByMe);

            // update owner
            owner!.Count--;
            owner = null;

        }

        void UpdateInParent(Node? parent, Node? replaceWith)
        {
            if (parent is null)
            {  // I'm root of tree, so update tree itself
                owner!.Root = replaceWith;
                if (replaceWith is not null)
                    replaceWith.Parent = null; // make sure no more tree roots
                return;
            }

            if (parent.Left == this)
                parent.Left = replaceWith;
            if (parent.Right == this)
                parent.Right = replaceWith;
            if (replaceWith is not null)
                replaceWith.Parent = parent; // make sure correct links

            var grandParent = parent.Parent;
            var newParent = parent.Balance();
            if (newParent != parent)
                UpdateInParent(grandParent, newParent);
        }

        private Node RotateLeft()
        {
            Node newParent = Right!;
            newParent.Parent = Parent;
            Right = newParent.Left;

            if (Right is not null)
                Right.Parent = this;

            newParent.Left = this;
            Parent = newParent;

            UpdateHeight();
            newParent.UpdateHeight();

            return newParent;
        }

        private Node RotateRight()
        {
            Node newParent = Left!;
            newParent.Parent = Parent;

            Left = newParent.Right;
            if (Left is not null)
                Left.Right = this;

            newParent.Right = this;
            Parent = newParent;

            UpdateHeight();
            newParent.UpdateHeight();

            return newParent;
        }

        private int LeftHeight => Left?.Height ?? 0;
        private int RightHeight => Right?.Height ?? 0;
        private void UpdateHeight()
        {
            Height = 1 + Math.Max(LeftHeight, RightHeight);
        }

        private int BalanceFactor => LeftHeight - RightHeight;

        private Node Balance()
        {
            UpdateHeight();
            int factor = BalanceFactor;
            if (factor > 1)
            {
                if (Left?.BalanceFactor < 0)
                    Left = Left.RotateLeft(); 
                return RotateRight();
            }
            if (factor < -1)
            {
                if (Right?.BalanceFactor > 0)
                    Right = Right.RotateRight();
                return RotateLeft();
            }
            return this;
        }

        public IEnumerator<T> GetEnumerator()
        {
            if (Left is not null)
                foreach (T item in Left)
                    yield return item;

            yield return Value;

            if (Right is not null)
                foreach (T item in Right) 
                    yield return item;
        }

        /// <summary>
        /// returns next node with higher value
        /// </summary>
        /// <returns></returns>
        public Node? Next() 
        {
            if (Right is not null)
                return Right.FindMostLeft();
            return Parent?.Left == this ? Parent : null;
        }

        /// <summary>
        /// returns previous node with lower value
        /// </summary>
        /// <returns></returns>
        public Node? Prev()
        {
            if (Left is not null)
                return Left.FindMostRight();
            return Parent?.Right == this ? Parent : null;
        }
    }
}
