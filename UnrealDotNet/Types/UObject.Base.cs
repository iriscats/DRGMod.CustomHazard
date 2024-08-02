using System.Globalization;
using System.Text.Json;

namespace UnrealDotNet.Types;

public partial class UObject
{
    #region _UE_4_27_

    public static int ObjectOuterOffset = 0x20;
    public static int ClassOffset = 0x10;
    public static int NameOffset = 0x18;
    public static int StructSuperOffset = 0x40;
    public static int ChildPropertiesOffset = 0x50;
    public static int ChildrenOffset = 0x48;
    public static int FieldNameOffset = 0x28;
    public static int FieldTypeNameOffset = 0;
    public static int FieldClassOffset = 0x8;
    public static int FieldNextOffset = 0x20;
    public static int FuncNextOffset = 0x20;
    public static int FieldOffset = 0x4C;
    public static int PropertySize = 0x78;
    public static int VTableFuncNum = 66;
    public static int FuncFlagsOffset = 0xB0;
    public static int EnumArrayOffset = 0x40;
    public static int EnumCountOffset = 0x48;

    #endregion

    public static string GetName(int key)
    {
        var unrealEngine = UnrealEngine.GetInstance();
        var namePtr = unrealEngine.MemoryReadPtr(unrealEngine.GNames + ((key >> 16) + 2) * 8);
        if (namePtr == 0)
            return "badIndex";

        var nameEntry = unrealEngine.MemoryReadShort(namePtr + (key & 0xffff) * 2);
        var nameLength = nameEntry >> 6;
        if (nameLength <= 0)
            return "badIndex";

        return unrealEngine.MemoryReadString(namePtr + (key & 0xffff) * 2 + 2, nameLength);
    }


    public static void FindUObjectOffset()
    {
        var unrealEngine = UnrealEngine.GetInstance();
        bool NewFName = true;
        var world = unrealEngine.MemoryReadPtr(unrealEngine.GWorld);
        {
            var foundClassAndName = false;
            for (var c = 0; c < 0x50 && !foundClassAndName; c += 0x8)
            {
                var classPtr = unrealEngine.MemoryReadPtr(world + c);
                if (classPtr == 0x0)
                    continue;

                for (var n = 0; n < 0x50 && !foundClassAndName; n += 0x8)
                {
                    var classNameIndex = unrealEngine.MemoryReadInt(classPtr + n);
                    var name = GetName(classNameIndex);
                    if (name == "World")
                    {
                        ClassOffset = c;
                        NameOffset = n;
                        foundClassAndName = true;
                    }
                }
            }

            if (!foundClassAndName)
                throw new Exception("bad World or offsets?");
        }


        {
            var foundOuter = false;
            var classPtr = unrealEngine.MemoryReadPtr(world + ClassOffset);
            for (var o = 0; o < 0x50; o += 0x8)
            {
                var outerObj = unrealEngine.MemoryReadPtr(classPtr + o);
                var classNameIndex = unrealEngine.MemoryReadInt(outerObj + NameOffset);
                var name = GetName(classNameIndex);
                if (name == "/Script/Engine")
                {
                    ObjectOuterOffset = o;
                    foundOuter = true;
                    break;
                }
            }

            if (!foundOuter)
                throw new Exception("bad outer addr");
        }


        {
            var foundSuper = false;
            var classPtr = unrealEngine.MemoryReadPtr(world + ClassOffset);
            for (var o = 0; o < 0x50; o += 0x8)
            {
                var superObj = unrealEngine.MemoryReadPtr(classPtr + o);
                var classNameIndex = unrealEngine.MemoryReadInt(superObj + NameOffset);
                var name = GetName(classNameIndex);
                if (name == "Object")
                {
                    StructSuperOffset = o;
                    foundSuper = true;
                    break;
                }
            }

            if (!foundSuper)
                throw new Exception("bad super addr");
        }
        {
            var foundChildrenAndFieldName = false;
            var classPtr = unrealEngine.MemoryReadPtr(world + ClassOffset);
            for (var c = 0; c < 0x80 && !foundChildrenAndFieldName; c += 0x8)
            {
                var childPtr = unrealEngine.MemoryReadPtr(classPtr + c);
                if (childPtr == 0x0)
                    continue;

                for (var n = 0; n < 0x80 && !foundChildrenAndFieldName; n += 0x8)
                {
                    var classNameIndex = unrealEngine.MemoryReadInt(childPtr + n);
                    var name = GetName(classNameIndex);
                    if (name == "PersistentLevel")
                    {
                        ChildPropertiesOffset = c;
                        FieldNameOffset = n;
                        foundChildrenAndFieldName = true;
                    }
                }
            }

            if (!foundChildrenAndFieldName)
                throw new Exception("bad childs offset");
        }

        {
            var foundNextField = false;
            var classPtr = unrealEngine.MemoryReadPtr(world + ClassOffset);
            var fieldPtr = unrealEngine.MemoryReadPtr(classPtr + ChildPropertiesOffset);
            for (var c = 0; c < 0x80 && !foundNextField; c += 0x8)
            {
                var childClassPtr = unrealEngine.MemoryReadPtr(fieldPtr + c);
                if (childClassPtr == 0x0)
                    continue;

                var classNameIndex = unrealEngine.MemoryReadInt(childClassPtr + FieldNameOffset);
                var name = GetName(classNameIndex);
                if (name == "NetDriver")
                {
                    FieldNextOffset = c;
                    foundNextField = true;
                }
            }

            if (!foundNextField)
                throw new Exception("bad next field offset");
        }
        {
            var foundFuncs = false;
            var classPtr = unrealEngine.MemoryReadPtr(world + ClassOffset);
            for (var c = 0; c < 0x80 && !foundFuncs; c += 0x8)
            {
                var childPtr = unrealEngine.MemoryReadPtr(classPtr + c);
                if (childPtr == 0x0)
                    continue;

                var classNameIndex = unrealEngine.MemoryReadInt(childPtr + NameOffset);
                var name = GetName(classNameIndex);
                if (name == "K2_GetWorldSettings")
                {
                    ChildrenOffset = c;
                    foundFuncs = true;
                }
            }

            if (!foundFuncs)
            {
                var testObj = new UObject(world);
                var isField = testObj["K2_GetWorldSettings"];
                if (isField != null)
                {
                    ChildrenOffset = ChildPropertiesOffset;
                    foundFuncs = true;
                }
            }

            if (!foundFuncs)
                throw new Exception("bad childs offset");
        }
        {
            var foundNextField = false;
            var classPtr = unrealEngine.MemoryReadPtr(world + ClassOffset);
            var fieldPtr = unrealEngine.MemoryReadPtr(classPtr + ChildrenOffset);
            for (var c = 0; c < 0x80 && !foundNextField; c += 0x8)
            {
                var childClassPtr = unrealEngine.MemoryReadPtr(fieldPtr + c);
                if (childClassPtr == 0x0)
                    continue;

                var classNameIndex = unrealEngine.MemoryReadInt(childClassPtr + NameOffset);
                var name = GetName(classNameIndex);
                if (name == "HandleTimelineScrubbed")
                {
                    FuncNextOffset = c;
                    foundNextField = true;
                }
            }

            if (!foundNextField)
                throw new Exception("bad next offset");
        }
        {
            var foundNextField = false;
            var classPtr = unrealEngine.MemoryReadPtr(world + ClassOffset);
            var fieldPtr = unrealEngine.MemoryReadPtr(classPtr + ChildPropertiesOffset);
            for (var c = 0; c < 0x80 && !foundNextField; c += 0x8)
            {
                var childClassPtr = unrealEngine.MemoryReadPtr(fieldPtr + c);
                if (childClassPtr == 0x0)
                    continue;

                var classNameOffset = NewFName ? 0 : FieldNameOffset;
                var classNameIndex = unrealEngine.MemoryReadInt(childClassPtr + classNameOffset);
                var name = GetName(classNameIndex);
                if (name == "ObjectProperty")
                {
                    FieldClassOffset = c;
                    foundNextField = true;
                }
            }

            if (!foundNextField)
                throw new Exception("bad field class offset");
        }
        {
            var foundFieldOffset = false;
            var classPtr = unrealEngine.MemoryReadPtr(world + ClassOffset);
            var fieldPtr = unrealEngine.MemoryReadPtr(classPtr + ChildPropertiesOffset);
            for (var c = 0x0; c < 0x80 && !foundFieldOffset; c += 0x4)
            {
                var fieldOffset = unrealEngine.MemoryReadPtr(fieldPtr + c);
                var nextFieldPtr = unrealEngine.MemoryReadPtr(fieldPtr + FieldNextOffset);
                var fieldOffsetPlus8 = unrealEngine.MemoryReadPtr(nextFieldPtr + c);
                if ((fieldOffset + 8) == fieldOffsetPlus8)
                {
                    FieldOffset = c;
                    foundFieldOffset = true;
                }
            }

            if (!foundFieldOffset)
                throw new Exception("bad field offset");
        }
        {
            var worldUObject = new UObject(world);
            var field = worldUObject.GetFieldAddr("StreamingLevelsToConsider");
            var foundPropertySize = false;
            for (var c = 0x60; c < 0x100 && !foundPropertySize; c += 0x8)
            {
                var classAddr = unrealEngine.MemoryReadPtr(field + c);
                var classNameIndex = unrealEngine.MemoryReadInt(classAddr + NameOffset);
                var name = GetName(classNameIndex);
                if (name == "StreamingLevelsToConsider")
                {
                    PropertySize = c;
                    foundPropertySize = true;
                }
            }

            if (!foundPropertySize)
                throw new Exception("bad property size offset");
        }
        {
            var vTable = unrealEngine.MemoryReadPtr(world);
            var foundProcessEventOffset = false;
            for (var i = 50; i < 0x100 && !foundProcessEventOffset; i++)
            {
                var s = unrealEngine.MemoryReadPtr(vTable + i * 8);
                var sig = (UInt64)unrealEngine.FindPattern("40 55 56 57 41 54 41 55 41 56 41 57", s, 0X20);
                if (sig != 0)
                {
                    VTableFuncNum = i;
                    foundProcessEventOffset = true;
                }
            }

            if (!foundProcessEventOffset)
                throw new Exception("bad process event offset");
        }
        {
            var testObj = new UObject(world);
            var funcAddr = testObj.GetFuncAddr(testObj.ClassAddress, testObj.ClassAddress, "K2_GetWorldSettings");
            var foundFuncFlags = false;
            for (var i = 0; i < 0x200 && !foundFuncFlags; i += 8)
            {
                var flags = unrealEngine.MemoryReadPtr(funcAddr + i);
                if (flags == (nint)0x0008000104020401)
                {
                    FuncFlagsOffset = i;
                    foundFuncFlags = true;
                }
            }

            if (!foundFuncFlags)
                throw new Exception("bad func flags offset");
        }
    }


    public override string ToString()
    {
        var tempEntity = ClassAddress;
        var fields = new List<object>
        {
            Capacity = 0
        };

        while (true)
        {
            var field = tempEntity + ChildPropertiesOffset - FieldNextOffset;
            while ((field = _unrealEngine.MemoryReadPtr(field + FieldNextOffset)) > 0)
            {
                var fName = GetName(_unrealEngine.MemoryReadInt(field + FieldNameOffset));
                var fType = GetFieldType(field);
                var fValue = "(" + field.ToString("X") + ")";
                var offset = GetFieldOffset(field);
                if (fType == "BoolProperty")
                {
                    fType = "Boolean";
                    fValue = this[fName].Flag.ToString();
                }
                else if (fType == "FloatProperty")
                {
                    fType = "Single";
                    fValue = BitConverter.ToSingle(BitConverter.GetBytes(this[fName].Value), 0)
                        .ToString(CultureInfo.InvariantCulture);
                }
                else if (fType == "DoubleProperty")
                {
                    fType = "Double";
                    fValue = BitConverter.ToDouble(BitConverter.GetBytes(this[fName].Value), 0)
                        .ToString(CultureInfo.InvariantCulture);
                }
                else if (fType == "IntProperty")
                {
                    fType = "Int32";
                    fValue = ((int)this[fName].Value).ToString("X");
                }
                else if (fType is "ObjectProperty" or "StructProperty")
                {
                    var structFieldIndex = _unrealEngine.MemoryReadInt(
                        _unrealEngine.MemoryReadPtr(field + PropertySize) +
                        NameOffset);
                    fType = GetName(structFieldIndex);
                }

                fields.Add(fType + " " + fName + " = " + fValue + " ( @ " + offset.ToString("X") + " - " +
                           (Address + offset).ToString("X") + " )");
            }

            tempEntity = _unrealEngine.MemoryReadPtr(tempEntity + StructSuperOffset);
            if (tempEntity == 0)
                break;
        }

        var obj = new
        {
            name = ClassName + " : " + GetFullPath(),
            hierarchy = GetHierachy(),
            fields
        };

        return JsonSerializer.Serialize(obj, new JsonSerializerOptions
        {
            IncludeFields = true,
            WriteIndented = true
        });
    }
}