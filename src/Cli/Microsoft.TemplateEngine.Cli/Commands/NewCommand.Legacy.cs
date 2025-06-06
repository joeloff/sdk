// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.CommandLine;
using System.CommandLine.Parsing;
using Microsoft.TemplateEngine.Abstractions;

namespace Microsoft.TemplateEngine.Cli.Commands
{
    internal partial class NewCommand : BaseCommand<NewCommandArgs>
    {
        private static readonly IReadOnlyList<FilterOptionDefinition> LegacyFilterDefinitions = new List<FilterOptionDefinition>()
        {
            FilterOptionDefinition.AuthorFilter,
            FilterOptionDefinition.BaselineFilter,
            FilterOptionDefinition.LanguageFilter,
            FilterOptionDefinition.TypeFilter,
            FilterOptionDefinition.TagFilter,
            FilterOptionDefinition.PackageFilter
        };

        internal IEnumerable<Option> LegacyOptions
        {
            get
            {
                yield return InteractiveOption;
                yield return AddSourceOption;
                yield return ColumnsAllOption;
                yield return ColumnsOption;
                foreach (Option filter in LegacyFilters.Values)
                {
                    yield return filter;
                }
            }
        }

        internal Option<bool> InteractiveOption { get; } = SharedOptionsFactory.CreateInteractiveOption().AsHidden();

        internal Option<string[]> AddSourceOption { get; } = SharedOptionsFactory.CreateAddSourceOption().AsHidden().DisableAllowMultipleArgumentsPerToken();

        internal Option<bool> ColumnsAllOption { get; } = SharedOptionsFactory.CreateColumnsAllOption().AsHidden();

        internal Option<string[]> ColumnsOption { get; } = SharedOptionsFactory.CreateColumnsOption().AsHidden().DisableAllowMultipleArgumentsPerToken();

        internal IReadOnlyDictionary<FilterOptionDefinition, Option> LegacyFilters { get; private set; } = new Dictionary<FilterOptionDefinition, Option>();

        internal void AddNoLegacyUsageValidators(Command command, params Symbol[] except)
        {
            IEnumerable<Option> optionsToVerify = LegacyFilters.Values.Concat(new Option[] { ColumnsAllOption, ColumnsOption, InteractiveOption, AddSourceOption });
            IEnumerable<Argument> argumentsToVerify = new Argument[] { ShortNameArgument, RemainingArguments };

            foreach (Option option in optionsToVerify)
            {
                if (!except.Contains(option))
                {
                    command.Validators.Add(symbolResult => ValidateOptionUsage(symbolResult, option));
                }
            }

            foreach (Argument argument in argumentsToVerify)
            {
                if (!except.Contains(argument))
                {
                    command.Validators.Add(symbolResult => ValidateArgumentUsage(symbolResult, argument));
                }
            }
        }

        internal void ValidateShortNameArgumentIsNotUsed(CommandResult commandResult)
        {
            ValidateArgumentUsage(commandResult, ShortNameArgument);
        }

        internal void ValidateArgumentsAreNotUsed(CommandResult commandResult)
        {
            ValidateArgumentUsage(commandResult, ShortNameArgument, RemainingArguments);
        }

        internal void ValidateOptionUsage(CommandResult commandResult, Option option)
        {
            if (commandResult.Parent is not CommandResult parentResult)
            {
                return;
            }

            OptionResult? optionResult = parentResult.Children.OfType<OptionResult>().FirstOrDefault(result => result.Option == option);
            if (optionResult != null)
            {
                List<string> wrongTokens = new();
                if (optionResult.IdentifierToken is { } && !string.IsNullOrWhiteSpace(optionResult.IdentifierToken.Value))
                {
                    wrongTokens.Add($"'{optionResult.IdentifierToken.Value}'");
                }
                foreach (var token in optionResult.Tokens)
                {
                    if (token is { } t && !string.IsNullOrWhiteSpace(t.Value))
                    {
                        wrongTokens.Add($"'{t.Value}'");
                    }
                }
                //Unrecognized command or argument(s): {0}
                commandResult.AddError(string.Format(LocalizableStrings.Commands_Validator_WrongTokens, string.Join(",", wrongTokens)));
            }
        }

        private static void ValidateArgumentUsage(CommandResult commandResult, params Argument[] arguments)
        {
            if (commandResult.Parent is not CommandResult parentResult)
            {
                return;
            }

            List<string> wrongTokens = new();
            foreach (Argument argument in arguments)
            {
                var newCommandArgument = parentResult.Children.OfType<ArgumentResult>().FirstOrDefault(result => result.Argument == argument);
                if (newCommandArgument == null)
                {
                    continue;
                }
                foreach (var token in newCommandArgument.Tokens)
                {
                    if (!string.IsNullOrWhiteSpace(token.Value))
                    {
                        wrongTokens.Add($"'{token.Value}'");
                    }
                }
            }
            if (wrongTokens.Any())
            {
                //Unrecognized command or argument(s): {0}
                commandResult.AddError(string.Format(LocalizableStrings.Commands_Validator_WrongTokens, string.Join(",", wrongTokens)));
            }
        }

        private void BuildLegacySymbols(Func<ParseResult, ITemplateEngineHost> hostBuilder)
        {
            Arguments.Add(ShortNameArgument);
            Arguments.Add(RemainingArguments);

            //legacy options
            Dictionary<FilterOptionDefinition, Option> options = new();
            foreach (var filterDef in LegacyFilterDefinitions)
            {
                options[filterDef] = filterDef.OptionFactory().AsHidden();
                Options.Add(options[filterDef]);
            }
            LegacyFilters = options;

            Options.Add(InteractiveOption);
            Options.Add(AddSourceOption);
            Options.Add(ColumnsAllOption);
            Options.Add(ColumnsOption);

            TreatUnmatchedTokensAsErrors = true;

            Add(new LegacyInstallCommand(this, hostBuilder));
            Add(new LegacyUninstallCommand(this, hostBuilder));
            Add(new LegacyUpdateCheckCommand(this, hostBuilder));
            Add(new LegacyUpdateApplyCommand(this, hostBuilder));
            Add(new LegacySearchCommand(this, hostBuilder));
            Add(new LegacyListCommand(this, hostBuilder));
            Add(new LegacyAliasAddCommand(hostBuilder));
            Add(new LegacyAliasShowCommand(hostBuilder));
        }
    }
}

