using FluentAssertions;
using System.Diagnostics;

using MyIndexType = int;

namespace SeriesCache.Tests;

public class AVLTreeTests
{
    static AVLTreeTests()
    {
        Initial = Build(Original);
    }


    [DebuggerDisplay("{Id}")]
    struct Item 
    {
        [DebuggerStepThrough]
        public Item(MyIndexType id)
        {
            Id = id;
        }
        public MyIndexType Id { get; init;}
    }

    class MyAVLTree : AVLTree<Item, MyIndexType>
    {
        private static readonly DistanceWithValueRef<Item, Item, MyIndexType> distanceFunc = (ref readonly Item item, ref readonly Item value) => item.Id - value.Id;
        public MyAVLTree() : base(distanceFunc)
        {
        }
    }

    [Test]
    public void AddShouldCauseCorrectOrder()
    {
        MyIndexType[] original = Original;
        List<MyIndexType> originalOrdered = new List<MyIndexType>(original);
        originalOrdered.Sort();

        var items = Build(original);
        var ordered = items.Select(item => item.Id).ToArray();

        ordered.Should().BeEquivalentTo(originalOrdered.ToArray(), o => o.WithStrictOrdering() );
    }

    [Test]
    public void SizeShouldMatch()
    {
        Initial.Count.Should().Be(Original.Length);
    }
    
    public struct NearestData
    {
        public MyIndexType ValueToTest;
        public MyIndexType NearestOriginalIndex;

        public void Deconstruct(out MyIndexType value, out MyIndexType original)
        {
            value = ValueToTest;
            original = Original[NearestOriginalIndex];
        }
    }


    private static readonly MyAVLTree Initial;
    private static readonly MyIndexType[] Original = {10, 5, 7, 25, 31 };

    static readonly NearestData[] NearestDataSet =
    {
        new NearestData { ValueToTest = 11, NearestOriginalIndex = 0 }, // 10
        new NearestData { ValueToTest =  7, NearestOriginalIndex = 2 }, // 7, will be a root of AVL tree
        new NearestData { ValueToTest =  5, NearestOriginalIndex = 1 }, // 5
        new NearestData { ValueToTest = 10, NearestOriginalIndex = 0 }, // 10
        new NearestData { ValueToTest = 31, NearestOriginalIndex = 4 }, // 31
        new NearestData { ValueToTest = 33, NearestOriginalIndex = 4 }, // 31, highest value
        new NearestData { ValueToTest = 29, NearestOriginalIndex = 4 }, // 31
        new NearestData { ValueToTest = 25, NearestOriginalIndex = 3 }, // 25
        new NearestData { ValueToTest = 28, NearestOriginalIndex = 3 }, // lower value (25) should be returned, instead of higher with same distance (31)
        new NearestData { ValueToTest =  1, NearestOriginalIndex = 1 }, // 5, lowest value
    };
    [TestCaseSource(nameof(NearestDataSet))]
    public void NerestShouldBeFound(NearestData data)
    {
        var (value, original) = data;
        Initial.FindNearest(new Item(value)).Value.Id.Should().Be(original, $"Nearest for {value} is {original}");
    }

    [TestCaseSource(nameof(Original))]
    public void DeleteShouldCauseCorrectOrder(MyIndexType valueToDelete)
    {
        var tree = Build(Original);
        var nodeToDelete = tree.FindNearest(new Item(valueToDelete));
        nodeToDelete.Value.Id.Should().Be(valueToDelete);

        nodeToDelete.Remove();
        ValidateNodesRelations(tree.Root!);

        tree.Count.Should().Be(Original.Length - 1);
        tree.FindNearest(nodeToDelete.Value).Value.Id.Should().NotBe(valueToDelete);

        var ordered = tree.Select(i => i.Id).ToArray();
        ordered.Length.Should().Be(Original.Length - 1);

        List<MyIndexType> originalOrdered = new List<MyIndexType>(Original);
        originalOrdered.Remove(valueToDelete);
        originalOrdered.Sort();

        ordered.Should().BeEquivalentTo(originalOrdered, o => o.WithoutStrictOrdering());
    }

    [Test]
    public void SequentialDeletionKeepsCorrectOrder()
    {
        var tree = Build(Original);
        for (int i = 0; i < Original.Length; i++) 
        { 
            var nodeToRemove = tree.FindNearest(new Item(Original[i]));
            nodeToRemove.Value.Id.Should().Be(Original[i]);
            nodeToRemove.Remove();

            tree.Count.Should().Be(Original.Length-i-1);
            
            if (i < Original.Length-1)
                ValidateNodesRelations(tree.Root!);
            else
                tree.Root.Should().BeNull();
        }
    }

    [Test]
    public void DeletedNodeShouldBeDetched()
    {
        const int valueToDelete = 10;
        var tree = Build(Original);
        var nodeToDelete = tree.FindNearest(new Item(valueToDelete));
        nodeToDelete.Remove();

        nodeToDelete.Value.Id.Should().Be(valueToDelete); // value must remain

        // references should be cleared
        nodeToDelete.Parent.Should().BeNull();
        nodeToDelete.Left.Should().BeNull();
        nodeToDelete.Right.Should().BeNull();

        Action removeOnceAgain = () => { nodeToDelete.Remove(); };
        removeOnceAgain.Should().ThrowExactly<InvalidOperationException>();

        Action addToDetached = () => { nodeToDelete.Add(new Item(1111)); };
        addToDetached.Should().ThrowExactly<InvalidOperationException>();
    }

    [Test]
    public void EmptyTreeCanNotBeSearchible()
    {
        var tree = Build(Array.Empty<MyIndexType>());
        Action search = () => { tree.FindNearest(new Item(111));};
        search.Should().ThrowExactly<InvalidOperationException>();
    }


    [Test]
    public void Duplcates()
    {
        MyIndexType[] original = [10, 20, 10, 30, 20, 30];
        var tree = Build(original);
        tree.Count.Should().Be(original.Length);

        var originalOrdered = new List<MyIndexType>(original);
        originalOrdered.Sort();
        var ordered = tree.Select(i => i.Id).ToArray();
        ordered.Should().BeEquivalentTo(originalOrdered, o=>o.WithStrictOrdering());

        tree.FindNearest(new Item( 1)).Value.Id.Should().Be(original[0]); // 10, lowest value
        tree.FindNearest(new Item(10)).Value.Id.Should().Be(original[0]); // 10
        tree.FindNearest(new Item(15)).Value.Id.Should().Be(original[0]); // 10, lower value
        tree.FindNearest(new Item(25)).Value.Id.Should().Be(original[1]); // 20, lower value
        tree.FindNearest(new Item(30)).Value.Id.Should().Be(original[3]); // 30
        tree.FindNearest(new Item(31)).Value.Id.Should().Be(original[3]); // 30, highest value
    }


    private static void ValidateNodesRelations(MyAVLTree.Node node)
    {
        ValidateParent(node.Left, node);
        ValidateParent(node.Right, node);
        if (node.Left is not null)
            ValidateNodesRelations(node.Left);
        if (node.Right is not null) 
            ValidateNodesRelations(node.Right);
    }

    private static void ValidateParent(MyAVLTree.Node? node, MyAVLTree.Node expectedParent)
    {
        if (node == null)
            return;
        if (node.Parent != expectedParent)
            throw new InvalidOperationException("Invalid parent detected");
    }

    private static MyAVLTree Build(params MyIndexType[] values)
    {
        var retVal = new MyAVLTree();
        foreach (MyIndexType id in values)
        {
            retVal.Add(new Item(id));
            ValidateNodesRelations(retVal.Root!);
        }
        return retVal;
    }
}