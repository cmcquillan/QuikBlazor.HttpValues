namespace QuikBlazor.HttpValues;

public class GraphQLQuery<TParam> : IEquatable<GraphQLQuery<TParam>?>
{
    public GraphQLQuery(string query, object variables)
    {
        Query = query;
        Variables = variables;
    }

    public string Query { get; set; } = string.Empty;

    public object Variables { get; set; } = new object();

    public override bool Equals(object? obj)
    {
        return Equals(obj as GraphQLQuery<TParam>);
    }

    public bool Equals(GraphQLQuery<TParam>? other)
    {
        return other is not null &&
               Query == other.Query &&
               EqualityComparer<object>.Default.Equals(Variables, other.Variables);
    }

    public override int GetHashCode()
    {
        return HashCode.Combine(Query, Variables);
    }

    public static bool operator ==(GraphQLQuery<TParam>? left, GraphQLQuery<TParam>? right)
    {
        return EqualityComparer<GraphQLQuery<TParam>>.Default.Equals(left, right);
    }

    public static bool operator !=(GraphQLQuery<TParam>? left, GraphQLQuery<TParam>? right)
    {
        return !(left == right);
    }
}