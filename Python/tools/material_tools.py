from mcp.server.fastmcp import FastMCP, Context
from typing import List, Optional
from unity_connection import get_unity_connection

def register_material_tools(mcp: FastMCP):
    """Register all material-related tools with the MCP server."""
    
    @mcp.tool()
    def set_material(
        ctx: Context,
        object_name: str,
        material_name: Optional[str] = None,
        color: Optional[List[float]] = None,
        create_if_missing: bool = True
    ) -> str:
        """
        Apply or create a material for a game object. If material_name is provided,
        the material will be saved as a shared asset in the Materials folder.
        
        Args:
            object_name: Target game object.
            material_name: Optional material name. If provided, creates/uses a shared material asset.
            color: Optional [R, G, B] or [R, G, B, A] values (0.0-1.0).
            create_if_missing: Whether to create the material if it doesn't exist (default: True).
            
        Returns:
            str: Status message indicating success or failure.
        """
        try:
            unity = get_unity_connection()
            
            # Check if the object exists
            object_response = unity.send_command("FIND_OBJECTS_BY_NAME", {
                "name": object_name
            })
            
            objects = object_response.get("objects", [])
            if not objects:
                return f"GameObject '{object_name}' not found in the scene."
            
            # If a material name is specified, check if it exists
            if material_name:
                material_assets = unity.send_command("GET_ASSET_LIST", {
                    "type": "Material",
                    "search_pattern": material_name,
                    "folder": "Assets/Materials"
                }).get("assets", [])
                
                material_exists = any(asset.get("name") == material_name for asset in material_assets)
                
                if not material_exists and not create_if_missing:
                    return f"Material '{material_name}' not found. Use create_if_missing=True to create it."
            
            # Validate color values if provided
            if color:
                # Check if color has the right number of components (RGB or RGBA)
                if not (len(color) == 3 or len(color) == 4):
                    return f"Error: Color must have 3 (RGB) or 4 (RGBA) components, but got {len(color)}."
                
                # Check if all color values are in the 0-1 range
                for i, value in enumerate(color):
                    if not isinstance(value, (int, float)):
                        return f"Error: Color component at index {i} is not a number."
                    
                    if value < 0.0 or value > 1.0:
                        channel = "RGBA"[i] if i < 4 else f"component {i}"
                        return f"Error: Color {channel} value must be in the range 0.0-1.0, but got {value}."
            
            # Set up parameters for the command
            params = {
                "object_name": object_name,
                "create_if_missing": create_if_missing
            }
            if material_name:
                params["material_name"] = material_name
            if color:
                params["color"] = color
                
            result = unity.send_command("SET_MATERIAL", params)
            material_name = result.get("material_name", "unknown")
            material_path = result.get("path")
            
            if material_path:
                return f"Applied shared material '{material_name}' to {object_name} (saved at {material_path})"
            else:
                return f"Applied instance material '{material_name}' to {object_name}"
                
        except Exception as e:
            return f"Error setting material: {str(e)}" 
        
    @mcp.tool()
    def create_advanced_material(
        ctx: Context,
        material_name: str,
        shader_type: str = "Standard",
        render_mode: str = "Opaque",
        save_path: str = "Assets/Materials",
        create_if_missing: bool = True
    ) -> str:
        """Create an advanced material with specific shader settings.
        
        Args:
            ctx: The MCP context
            material_name: Material name
            shader_type: Type of shader to use (Standard, Transparent, Unlit, etc.)
            render_mode: Render mode (Opaque, Transparent, Cutout)
            save_path: Path to save the material
            create_if_missing: Whether to create if already exists
            
        Returns:
            str: Result message
        """
        try:
            response = get_unity_connection().send_command("CREATE_ADVANCED_MATERIAL", {
                "material_name": material_name,
                "shader_type": shader_type,
                "render_mode": render_mode,
                "save_path": save_path,
                "create_if_missing": create_if_missing
            })
            return response.get("message", "Material created successfully")
        except Exception as e:
            return f"Error creating material: {str(e)}"

    @mcp.tool()
    def set_material_properties(
        ctx: Context,
        material_path: str,
        color: Optional[List[float]] = None,
        metallic: Optional[float] = None,
        smoothness: Optional[float] = None,
        normal_scale: Optional[float] = None,
        occlusion_strength: Optional[float] = None,
        height_scale: Optional[float] = None,
        emission_color: Optional[List[float]] = None,
        emission_intensity: Optional[float] = None
    ) -> str:
        """Set physical properties of a material.
        
        Args:
            ctx: The MCP context
            material_path: Path to the material
            color: [r, g, b] or [r, g, b, a] color values (0.0-1.0)
            metallic: Metallic value (0.0-1.0)
            smoothness: Smoothness/glossiness value (0.0-1.0)
            normal_scale: Normal map intensity
            occlusion_strength: Ambient occlusion strength (0.0-1.0)
            height_scale: Height/parallax map scale
            emission_color: [r, g, b] emission color values (0.0-1.0)
            emission_intensity: Emission intensity multiplier
            
        Returns:
            str: Result message
        """
        try:
            params = {"material_path": material_path}
            if color is not None: params["color"] = color
            if metallic is not None: params["metallic"] = metallic
            if smoothness is not None: params["smoothness"] = smoothness
            if normal_scale is not None: params["normal_scale"] = normal_scale
            if occlusion_strength is not None: params["occlusion_strength"] = occlusion_strength
            if height_scale is not None: params["height_scale"] = height_scale
            if emission_color is not None: params["emission_color"] = emission_color
            if emission_intensity is not None: params["emission_intensity"] = emission_intensity
            
            response = get_unity_connection().send_command("SET_MATERIAL_PROPERTIES", params)
            return response.get("message", "Material properties updated successfully")
        except Exception as e:
            return f"Error setting material properties: {str(e)}"

    @mcp.tool()
    def set_material_texture(
        ctx: Context,
        material_path: str,
        texture_type: str,
        texture_path: str,
        tiling: Optional[List[float]] = None,
        offset: Optional[List[float]] = None
    ) -> str:
        """Set a texture on a material.
        
        Args:
            ctx: The MCP context
            material_path: Path to the material
            texture_type: Type of texture (albedo, normal, metallic, etc.)
            texture_path: Path to the texture
            tiling: [x, y] tiling values
            offset: [x, y] offset values
            
        Returns:
            str: Result message
        """
        try:
            params = {
                "material_path": material_path,
                "texture_type": texture_type,
                "texture_path": texture_path
            }
            
            if tiling is not None: params["tiling"] = tiling
            if offset is not None: params["offset"] = offset
            
            response = get_unity_connection().send_command("SET_MATERIAL_TEXTURE", params)
            return response.get("message", "Material texture set successfully")
        except Exception as e:
            return f"Error setting material texture: {str(e)}"

    @mcp.tool()
    def create_material_from_template(
        ctx: Context,
        material_name: str,
        template: str,
        save_path: str = "Assets/Materials"
    ) -> str:
        """Create a material based on a predefined template.
            
        Args:
            ctx: The MCP context
            material_name: Name for the new material
            template: Template to use (metal, plastic, wood, glass, emissive, fabric, skin)
            save_path: Path to save the material
                
        Returns:
            str: Result message
        """
        try:
            response = get_unity_connection().send_command("CREATE_MATERIAL_FROM_TEMPLATE", {
                "material_name": material_name,
                "template": template,
                "save_path": save_path
            })
            return response.get("message", "Material created successfully")
        except Exception as e:
            return f"Error creating material from template: {str(e)}"