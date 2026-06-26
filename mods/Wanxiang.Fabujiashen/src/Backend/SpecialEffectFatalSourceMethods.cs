using System.Diagnostics.CodeAnalysis;
using System.Reflection;
using System.Reflection.Emit;
using GameData.Combat.Math;
using GameData.Common;
using GameData.Domains.Combat;
using GameData.Domains.SpecialEffect;
using HarmonyLib;

namespace Wanxiang.Fabujiashen.Backend;

internal static class SpecialEffectFatalSourceMethods
{
    private static readonly OpCode[] OneByteOpCodes = BuildOpCodeLookup(oneByte: true);

    private static readonly OpCode[] TwoByteOpCodes = BuildOpCodeLookup(oneByte: false);

    private static readonly MethodInfo[] FatalEntryMethods =
    [
        AccessTools.Method(
            typeof(CombatCharacter),
            nameof(CombatCharacter.AddFatalDamage),
            [
                typeof(DataContext),
                typeof(int),
                typeof(int),
                typeof(sbyte),
                typeof(short),
                typeof(EDamageType),
            ])
        ?? throw new MissingMethodException(
            nameof(CombatCharacter),
            nameof(CombatCharacter.AddFatalDamage)),
        AccessTools.Method(
            typeof(CombatCharacter),
            nameof(CombatCharacter.AddFatalDamageByMarkPercent),
            [typeof(DataContext), typeof(CValuePercent)])
        ?? throw new MissingMethodException(
            nameof(CombatCharacter),
            nameof(CombatCharacter.AddFatalDamageByMarkPercent)),
        AccessTools.Method(
            typeof(CombatCharacter),
            nameof(CombatCharacter.AddFatalMark),
            [
                typeof(DataContext),
                typeof(int),
                typeof(int),
                typeof(sbyte),
                typeof(bool),
                typeof(EDamageType),
            ])
        ?? throw new MissingMethodException(
            nameof(CombatCharacter),
            nameof(CombatCharacter.AddFatalMark)),
        AccessTools.Method(
            typeof(CombatCharacter),
            nameof(CombatCharacter.AddFatalMarkImmediate),
            [typeof(DataContext), typeof(int)])
        ?? throw new MissingMethodException(
            nameof(CombatCharacter),
            nameof(CombatCharacter.AddFatalMarkImmediate)),
        AccessTools.Method(
            typeof(CombatCharacter),
            nameof(CombatCharacter.TransferFatalMark),
            [typeof(DataContext), typeof(CombatCharacter), typeof(int)])
        ?? throw new MissingMethodException(
            nameof(CombatCharacter),
            nameof(CombatCharacter.TransferFatalMark)),
    ];

    internal static IEnumerable<MethodBase> Enumerate()
    {
        Type specialEffectBaseType = typeof(SpecialEffectBase);
        foreach (Type type in specialEffectBaseType.Assembly.GetTypes())
        {
            if (!specialEffectBaseType.IsAssignableFrom(type))
            {
                continue;
            }

            foreach (MethodInfo method in type.GetMethods(
                         BindingFlags.Instance
                         | BindingFlags.Public
                         | BindingFlags.NonPublic
                         | BindingFlags.DeclaredOnly))
            {
                if (!method.IsAbstract && CallsAnyFatalEntryMethod(method))
                {
                    yield return method;
                }
            }
        }
    }

    private static bool CallsAnyFatalEntryMethod(MethodBase method)
    {
        MethodBody? body = method.GetMethodBody();
        byte[]? il = body?.GetILAsByteArray();
        if (il is null || il.Length == 0)
        {
            return false;
        }

        int offset = 0;
        while (offset < il.Length)
        {
            OpCode opCode = ReadOpCode(il, ref offset);
            if (opCode.OperandType == OperandType.InlineMethod)
            {
                if (TryResolveMethod(method, il, ref offset, out MethodBase? calledMethod)
                    && FatalEntryMethods.Any(fatalEntryMethod => IsSameMethod(calledMethod, fatalEntryMethod)))
                {
                    return true;
                }

                continue;
            }

            offset += GetOperandSize(opCode.OperandType, il, offset);
        }

        return false;
    }

    private static bool TryResolveMethod(
        MethodBase owner,
        byte[] il,
        ref int offset,
        [NotNullWhen(true)] out MethodBase? method)
    {
        int token = BitConverter.ToInt32(il, offset);
        offset += sizeof(int);
        try
        {
            method = owner.Module.ResolveMethod(
                token,
                owner.DeclaringType?.GetGenericArguments(),
                owner.IsGenericMethod ? owner.GetGenericArguments() : Type.EmptyTypes);
            return method is not null;
        }
        catch (ArgumentException)
        {
            method = null;
            return false;
        }
    }

    private static bool IsSameMethod(MethodBase left, MethodBase right)
    {
        if (left.Module == right.Module && left.MetadataToken == right.MetadataToken)
        {
            return true;
        }

        ParameterInfo[] leftParameters = left.GetParameters();
        ParameterInfo[] rightParameters = right.GetParameters();
        return left.DeclaringType == right.DeclaringType
            && left.Name == right.Name
            && leftParameters.Length == rightParameters.Length
            && leftParameters
                .Select(parameter => parameter.ParameterType)
                .SequenceEqual(rightParameters.Select(parameter => parameter.ParameterType));
    }

    private static OpCode ReadOpCode(byte[] il, ref int offset)
    {
        byte value = il[offset++];
        if (value != 0xfe)
        {
            return OneByteOpCodes[value];
        }

        return TwoByteOpCodes[il[offset++]];
    }

    private static int GetOperandSize(OperandType operandType, byte[] il, int offset)
    {
        if (operandType == OperandType.InlineNone)
        {
            return 0;
        }

        if (operandType is OperandType.ShortInlineBrTarget
            or OperandType.ShortInlineI
            or OperandType.ShortInlineVar)
        {
            return 1;
        }

        if (operandType == OperandType.InlineVar)
        {
            return 2;
        }

        if (operandType is OperandType.InlineBrTarget
            or OperandType.InlineField
            or OperandType.InlineI
            or OperandType.InlineMethod
            or OperandType.InlineSig
            or OperandType.InlineString
            or OperandType.InlineTok
            or OperandType.InlineType
            or OperandType.ShortInlineR)
        {
            return 4;
        }

        if (operandType is OperandType.InlineI8 or OperandType.InlineR)
        {
            return 8;
        }

        if (operandType == OperandType.InlineSwitch)
        {
            return sizeof(int) + (BitConverter.ToInt32(il, offset) * sizeof(int));
        }

        throw new ArgumentOutOfRangeException(nameof(operandType), operandType, null);
    }

    private static OpCode[] BuildOpCodeLookup(bool oneByte)
    {
        OpCode[] result = new OpCode[256];
        foreach (FieldInfo field in typeof(OpCodes).GetFields(BindingFlags.Public | BindingFlags.Static))
        {
            if (field.GetValue(null) is not OpCode opCode)
            {
                continue;
            }

            ushort value = unchecked((ushort)opCode.Value);
            if (oneByte && value <= byte.MaxValue)
            {
                result[value] = opCode;
            }
            else if (!oneByte && (value & 0xff00) == 0xfe00)
            {
                result[value & 0xff] = opCode;
            }
        }

        return result;
    }
}
