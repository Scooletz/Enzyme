using System;
using System.Buffers.Binary;
using System.Reflection;
using System.Reflection.Emit;

namespace Enzyme
{
    static class EmitExtensions
    {
        public static void LoadIntValue(this ILGenerator il, int number)
        {
            switch (number)
            {
                case 0:
                    il.Emit(OpCodes.Ldc_I4_0);
                    return;
                case 1:
                    il.Emit(OpCodes.Ldc_I4_1);
                    return;
                case 2:
                    il.Emit(OpCodes.Ldc_I4_2);
                    return;
                case 3:
                    il.Emit(OpCodes.Ldc_I4_3);
                    return;
                case 4:
                    il.Emit(OpCodes.Ldc_I4_4);
                    return;
                case 5:
                    il.Emit(OpCodes.Ldc_I4_5);
                    return;
                case 6:
                    il.Emit(OpCodes.Ldc_I4_6);
                    return;
                case 7:
                    il.Emit(OpCodes.Ldc_I4_7);
                    return;
                case 8:
                    il.Emit(OpCodes.Ldc_I4_8);
                    return;
                default:
                    if (sbyte.MinValue <= number && number <= sbyte.MaxValue)
                    {
                        il.Emit(OpCodes.Ldc_I4_S, (sbyte)number);
                        return;
                    }
                    else
                    {
                        il.Emit(OpCodes.Ldc_I4, number);
                        return;
                    }
            }
        }

        public static void Load(this ILGenerator il, LocalBuilder var) => il.LoadVariable(var.LocalIndex);

        static void LoadVariable(this ILGenerator il, int number)
        {
            switch (number)
            {
                case 0:
                    il.Emit(OpCodes.Ldloc_0);
                    return;
                case 1:
                    il.Emit(OpCodes.Ldloc_1);
                    return;
                case 2:
                    il.Emit(OpCodes.Ldloc_2);
                    return;
                case 3:
                    il.Emit(OpCodes.Ldloc_3);
                    return;
                default:
                    if (number <= byte.MaxValue)
                    {
                        il.Emit(OpCodes.Ldloc_S, (sbyte)number);
                        return;
                    }
                    else
                    {
                        il.Emit(OpCodes.Ldloc, number);
                        return;
                    }
            }
        }

        public static void LoadAddress(this ILGenerator il, LocalBuilder var)
        {
            if (var.LocalIndex <= 255)
            {
                il.Emit(OpCodes.Ldloca_S, (byte)var.LocalIndex);
            }
            else
            {
                il.Emit(OpCodes.Ldloca, var.LocalIndex);
            }
        }

        public static void Store(this ILGenerator il, LocalBuilder var)
        {
            il.StoreVariable(var.LocalIndex);
        }

        static void StoreVariable(this ILGenerator il, int number)
        {
            switch (number)
            {
                case 0:
                    il.Emit(OpCodes.Stloc_0);
                    return;
                case 1:
                    il.Emit(OpCodes.Stloc_1);
                    return;
                case 2:
                    il.Emit(OpCodes.Stloc_2);
                    return;
                case 3:
                    il.Emit(OpCodes.Stloc_3);
                    return;
                default:
                    if (number <= byte.MaxValue)
                    {
                        il.Emit(OpCodes.Stloc_S, (sbyte)number);
                        return;
                    }
                    else
                    {
                        il.Emit(OpCodes.Stloc, number);
                        return;
                    }
            }
        }

        public static void CallOn(this ILGenerator il, LocalBuilder callee, MethodInfo method)
        {
            if (callee.LocalType.IsValueType)
            {
                il.LoadAddress(callee);
                il.EmitCall(OpCodes.Call, method, null);
            }
            else
            {
                il.Load(callee);
                il.EmitCall(OpCodes.Callvirt, method, null);
            }
        }

        public static void LoadElem(this ILGenerator il, Type type)
        {
            var opCode = GetLoadElem(type);
            if (opCode == OpCodes.Ldelem)
            {
                il.Emit(opCode, type);
            }
            else
            {
                il.Emit(opCode);
            }
        }

        static OpCode GetLoadElem(Type type)
        {
            if (type == typeof(byte))
            {
                return OpCodes.Ldelem_U1;
            }

            if (type == typeof(sbyte))
            {
                return OpCodes.Ldelem_I1;
            }

            if (type == typeof(short))
            {
                return OpCodes.Ldelem_I2;
            }

            if (type == typeof(ushort))
            {
                return OpCodes.Ldelem_U2;
            }

            if (type == typeof(int))
            {
                return OpCodes.Ldelem_I4;
            }

            if (type == typeof(uint))
            {
                return OpCodes.Ldelem_U4;
            }

            if (type == typeof(long))
            {
                return OpCodes.Ldelem_I8;
            }

            return OpCodes.Ldelem;
        }

        public static void LoadIndirect(this ILGenerator il, Type type)
        {
            il.Emit(GetLoadIndirect(type));
        }

        static OpCode GetLoadIndirect(Type type)
        {
            if (type == typeof(byte))
            {
                return OpCodes.Ldind_U1;
            }

            if (type == typeof(sbyte))
            {
                return OpCodes.Ldind_I1;
            }

            if (type == typeof(short))
            {
                return OpCodes.Ldind_I2;
            }

            if (type == typeof(ushort))
            {
                return OpCodes.Ldind_U2;
            }

            if (type == typeof(int))
            {
                return OpCodes.Ldind_I4;
            }

            if (type == typeof(uint))
            {
                return OpCodes.Ldind_U4;
            }

            if (type == typeof(long) || type == typeof(ulong))
            {
                return OpCodes.Ldind_I8;
            }

            return OpCodes.Ldind_Ref;
        }

        public static void StoreIndirectLittleEndian(this ILGenerator il, Type type)
        {
            if (BitConverter.IsLittleEndian == false)
            {
                var reverse = typeof(BinaryPrimitives).GetMethod(nameof(BinaryPrimitives.ReverseEndianness), new[] { type });
                il.EmitCall(OpCodes.Call, reverse, null);
            }

            il.Emit(GetStoreIndirect(type));
        }

        static OpCode GetStoreIndirect(Type type)
        {
            if (type == typeof(byte) || type == typeof(sbyte))
            {
                return OpCodes.Stind_I1;
            }

            if (type == typeof(short) || type == typeof(ushort))
            {
                return OpCodes.Stind_I2;
            }

            if (type == typeof(int) || type == typeof(uint))
            {
                return OpCodes.Stind_I4;
            }

            if (type == typeof(long) || type == typeof(ulong))
            {
                return OpCodes.Stind_I8;
            }

            throw new ArgumentException($"'{type.FullName}' is not supported", nameof(type));
        }
    }
}