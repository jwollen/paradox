// Copyright (c) 2014 Silicon Studio Corp. (http://siliconstudio.co.jp)
// This file is distributed under GPL v3. See LICENSE.md for details.
#include "stdafx.h"

using namespace System;
using namespace System::Collections::Generic;
using namespace System::IO;
using namespace SiliconStudio::Core::Diagnostics;
using namespace SiliconStudio::Core::Mathematics;
using namespace SiliconStudio::Core::Serialization;
using namespace SiliconStudio::Paradox::Rendering;
using namespace SiliconStudio::Paradox::Rendering::Materials;
using namespace SiliconStudio::Paradox::Rendering::Materials::ComputeColors;
using namespace SiliconStudio::Paradox::Assets::Materials;
using namespace SiliconStudio::Paradox::Engine;
using namespace SiliconStudio::Paradox::Graphics;
using namespace SiliconStudio::Paradox::Shaders;

namespace SiliconStudio { namespace Paradox { namespace Importer { namespace Common {
	
public ref class MeshParameters
{
public:
	MeshParameters()
	{
		Parameters = gcnew ParameterCollection();
	}

	ParameterCollection^ Parameters;
	String^ MaterialName;
	String^ MeshName;
	String^ NodeName;
};

public ref class NodeInfo
{
public:
	String^ Name;
	int Depth;
	bool Preserve;
};

public ref class EntityInfo
{
public:
	List<String^>^ TextureDependencies;
	Dictionary<String^, MaterialAsset^>^ Materials;
	List<String^>^ AnimationNodes;
	List<MeshParameters^>^ Models;
	List<NodeInfo^>^ Nodes;
};

public ref class MeshMaterials
{
public:
	Dictionary<String^, MaterialAsset^>^ Materials;
	List<MeshParameters^>^ Models;
};

public ref class TextureLayerGenerator
{
public:

	static ShaderClassSource^ GenerateTextureLayer(String^ vfsOutputPath, String^ sourceTextureFile, int textureUVSetIndex, Vector2 textureUVscaling , 
										int& textureCount, ParameterKey<Texture^>^ surfaceMaterialKey, 
										Mesh^ meshData, Logger^ logger)
	{
		ParameterKey<Texture^>^ parameterKey;

		auto texture = gcnew ContentReference<Texture^>();

		auto url = vfsOutputPath + "_" + Path::GetFileNameWithoutExtension(sourceTextureFile);

		texture->Location = url;
		//assetManager->Url->Set(texture, url);

		if (File::Exists(sourceTextureFile))
		{
			if (logger != nullptr)
			{
				logger->Warning("The texture '{0}' referenced in the mesh material can not be found on the system. Loading will probably fail at run time.", sourceTextureFile,
								nullptr, CallerInfo::Get(__FILEW__, __FUNCTIONW__, __LINE__));
			}
		}

		//meshData->Parameters->Set(
		//	parameterKey = ParameterKeys::IndexedKey(surfaceMaterialKey, textureCount++),
		//	texture);

		parameterKey = ParameterKeys::IndexedKey(surfaceMaterialKey, textureCount++);
		String^ uvSetName = "TEXCOORD";
		if (textureUVSetIndex != 0)
			uvSetName += textureUVSetIndex;
		//albedoMaterial->Add(gcnew ShaderClassSource("TextureStream", uvSetName, "TEXTEST" + uvSetIndex));
		auto uvScaling = textureUVscaling;
		auto textureName = parameterKey->Name;
		auto needScaling = uvScaling != Vector2::One;
		auto currentComposition = needScaling
			? gcnew ShaderClassSource("ComputeColorTextureRepeat", textureName, uvSetName, "float2(" + uvScaling[0] + ", " + uvScaling[1] + ")")
			: gcnew ShaderClassSource("ComputeColorTexture", textureName, uvSetName);

		return currentComposition;
	}

	static ComputeTextureColor^ GenerateMaterialTextureNode(String^ vfsOutputPath, String^ sourceTextureFile, int textureUVSetIndex, Vector2 textureUVscaling, bool wrapTextureU, bool wrapTextureV, Logger^ logger)
	{
		auto textureFileName = Path::GetFileNameWithoutExtension(sourceTextureFile);
		auto url = vfsOutputPath + "_" + textureFileName;

		if (File::Exists(sourceTextureFile))
		{
			if (logger != nullptr)
			{
				logger->Warning("The texture '{0}' referenced in the mesh material can not be found on the system. Loading will probably fail at run time.", sourceTextureFile,
								nullptr, CallerInfo::Get(__FILEW__, __FUNCTIONW__, __LINE__));
			}
		}

		auto uvScaling = textureUVscaling;
		auto textureName = textureFileName;
	
		auto texture = AttachedReferenceManager::CreateSerializableVersion<Texture^>(System::Guid(), textureName);

		auto currentTexture = gcnew ComputeTextureColor(texture, (TextureCoordinate)textureUVSetIndex, uvScaling, Vector2::Zero);
		currentTexture->AddressModeU = wrapTextureU ? TextureAddressMode::Wrap : TextureAddressMode::Clamp;
		currentTexture->AddressModeV = wrapTextureV ? TextureAddressMode::Wrap : TextureAddressMode::Clamp;
	
		return currentTexture;
	}
};
}}}}