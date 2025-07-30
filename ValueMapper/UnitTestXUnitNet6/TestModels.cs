using System;
using System.Collections.Generic;
using ValueMapperUtility;
using ValueMapperUtility.Attribute;

namespace UnitTestXUnitNet6
{
    public enum SampleEnum
    {
        None,
        Value1,
        Value2
    }
    public class Models
    {

        public int IntValue { get; set; }

        public double DoubleValue { get; set; }
        public string StringNumber { get; set; }
        public string StringValue { get; set; }
    }
    public class NumericDestination
    {
        [ValueMapperMapping("IntValue")]
        public long LongValue { get; set; }

        [ValueMapperMapping("DoubleValue")]
        public float FloatValue { get; set; }

        [ValueMapperMapping("StringNumber")]
        public int IntFromString { get; set; }
    }

    public class BasicSource
    {
        public string Value { get; set; }
    }

    public class NullableDestination
    {
        public int? NullableValue { get; set; }
    }

    public class NonNullableDestination
    {
        public int IntValue { get; set; }
    }

    // Custom class with ValueMapperIgnoreAttribute
    public class SourceWithIgnoredProperties
    {
        public string Name { get; set; }

        [ValueMapperIgnore]
        public string IgnoredProperty { get; set; }
    }

    public class DestinationWithIgnore
    {
        public string Name { get; set; }
        public string IgnoredProperty { get; set; }
    }

    public class UserWithRoles
    {
        public string Name { get; set; }
        public List<string> Roles { get; set; }
    }

    public class UserWithRolesDestination
    {
        public string Name { get; set; }
        public List<string> Roles { get; set; }
    }

    public class DeepSourceObject
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public DeepSourceChild Child { get; set; }
    }

    public class DeepSourceChild
    {
        public int ChildId { get; set; }
        public string Description { get; set; }
        public Dictionary<string, string> Metadata { get; set; }
        public List<string> Tags { get; set; }
        public DeepSourceGrandChild GrandChild { get; set; }
    }

    public class DeepSourceGrandChild
    {
        public int GrandChildId { get; set; }
        public bool IsActive { get; set; }
        public DateTime CreatedDate { get; set; }
    }

    public class DeepDestinationObject
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public DeepDestinationChild Child { get; set; }
    }

    public class DeepDestinationChild
    {
        public int ChildId { get; set; }
        public string Description { get; set; }
        public Dictionary<string, string> Metadata { get; set; }
        public List<string> Tags { get; set; }
        public DeepDestinationGrandChild GrandChild { get; set; }
    }

    public class DeepDestinationGrandChild
    {
        public int GrandChildId { get; set; }
        public bool IsActive { get; set; }
        public DateTime CreatedDate { get; set; }
    }

    public class FlatSourceObject
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public int ChildId { get; set; }
        public string ChildDescription { get; set; }
        public string ChildMetadataKey1 { get; set; }
        public string ChildMetadataKey2 { get; set; }
        public string ChildTag1 { get; set; }
        public string ChildTag2 { get; set; }
        public int GrandChildId { get; set; }
        public bool GrandChildIsActive { get; set; }
        public DateTime GrandChildCreatedDate { get; set; }
    }

    public class FlatDestinationObject
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public int ChildId { get; set; }
        public string ChildDescription { get; set; }
        public string ChildMetadataKey1 { get; set; }
        public string ChildMetadataKey2 { get; set; }
        public string ChildTag1 { get; set; }
        public string ChildTag2 { get; set; }
        public int GrandChildId { get; set; }
        public bool GrandChildIsActive { get; set; }
        public DateTime GrandChildCreatedDate { get; set; }
    }

    // Models for testing complex collections in deep object mapping
    public class ComplexMetadataItem
    {
        public int Id { get; set; }
        public string Value { get; set; }
    }

    public class ComplexTag
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public bool IsActive { get; set; }
    }

    public class DeepSourceObjectWithComplexCollections
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public DeepSourceChildWithComplexCollections Child { get; set; }
    }

    public class DeepSourceChildWithComplexCollections
    {
        public int ChildId { get; set; }
        public string Description { get; set; }
        public Dictionary<string, ComplexMetadataItem> ComplexMetadata { get; set; }
        public List<ComplexTag> ComplexTags { get; set; }
        public DeepSourceGrandChild GrandChild { get; set; }
    }

    public class DeepDestinationObjectWithComplexCollections
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public DeepDestinationChildWithComplexCollections Child { get; set; }
    }

    public class DeepDestinationChildWithComplexCollections
    {
        public int ChildId { get; set; }
        public string Description { get; set; }
        public Dictionary<string, ComplexMetadataItem> ComplexMetadata { get; set; }
        public List<ComplexTag> ComplexTags { get; set; }
        public DeepDestinationGrandChild GrandChild { get; set; }
    }

    // Models for testing nested collections in deep object mapping
    public class DeepSourceObjectWithNestedCollections
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public DeepSourceChildWithNestedCollections Child { get; set; }
    }

    public class DeepSourceChildWithNestedCollections
    {
        public int ChildId { get; set; }
        public string Description { get; set; }
        public Dictionary<string, List<string>> NestedMetadata { get; set; }
        public List<List<string>> NestedTags { get; set; }
        public DeepSourceGrandChild GrandChild { get; set; }
    }

    public class DeepDestinationObjectWithNestedCollections
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public DeepDestinationChildWithNestedCollections Child { get; set; }
    }

    public class DeepDestinationChildWithNestedCollections
    {
        public int ChildId { get; set; }
        public string Description { get; set; }
        public Dictionary<string, List<string>> NestedMetadata { get; set; }
        public List<List<string>> NestedTags { get; set; }
        public DeepDestinationGrandChild GrandChild { get; set; }
    }
} 