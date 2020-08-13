using Catalyst.Abstractions.Validators;
using System.Collections.Generic;

public class ListValidatorSet : IValidatorSet
{
    private IEnumerable<string> _validators;
    public int StartBlock { get; }

    public ListValidatorSet(int startBlock, IEnumerable<string> validators)
    {
        StartBlock = startBlock;
        _validators = validators;
    }

    public IEnumerable<string> GetValidators() => _validators;
}
