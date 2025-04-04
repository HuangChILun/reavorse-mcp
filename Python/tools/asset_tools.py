import os
import tempfile
import httpx
from typing import Optional, List
from mcp.server.fastmcp import FastMCP, Context
from unity_connection import get_unity_connection

from typing import Optional
from mcp.server.fastmcp import FastMCP, Context
from unity_connection import get_unity_connection

def register_asset_tools(mcp: FastMCP):
    """Register all asset management tools with the MCP server."""
    
    @mcp.tool()
    def import_asset(
        ctx: Context,
        source_path: str,
        target_path: str,
        overwrite: bool = False
    ) -> str:
        """Import an asset (e.g., 3D model, texture) into the Unity project.

        Args:
            ctx: The MCP context
            source_path: Path to the source file on disk
            target_path: Path where the asset should be imported in the Unity project (relative to Assets folder)
            overwrite: Whether to overwrite if an asset already exists at the target path (default: False)

        Returns:
            str: Success message or error details
        """
        try:
            unity = get_unity_connection()
            
            # Parameter validation
            if not source_path or not isinstance(source_path, str):
                return f"Error importing asset: source_path must be a valid string"
            
            if not target_path or not isinstance(target_path, str):
                return f"Error importing asset: target_path must be a valid string"
            
            # Check if the source file exists (on local disk)
            import os
            if not os.path.exists(source_path):
                return f"Error importing asset: Source file '{source_path}' does not exist"
            
            # Extract the target directory and filename
            target_dir = '/'.join(target_path.split('/')[:-1])
            target_filename = target_path.split('/')[-1]
            
            # Check if an asset already exists at the target path
            existing_assets = unity.send_command("GET_ASSET_LIST", {
                "search_pattern": target_filename,
                "folder": target_dir or "Assets"
            }).get("assets", [])
            
            # Check if any asset matches the exact path
            asset_exists = any(asset.get("path") == target_path for asset in existing_assets)
            if asset_exists and not overwrite:
                return f"Asset already exists at '{target_path}'. Use overwrite=True to replace it."
                
            response = unity.send_command("IMPORT_ASSET", {
                "source_path": source_path,
                "target_path": target_path,
                "overwrite": overwrite
            })
            
            if not response.get("success", False):
                return f"Error importing asset: {response.get('error', 'Unknown error')} (Source: {source_path}, Target: {target_path})"
                
            return response.get("message", "Asset imported successfully")
        except Exception as e:
            return f"Error importing asset: {str(e)} (Source: {source_path}, Target: {target_path})"

    @mcp.tool()
    def instantiate_prefab(
        ctx: Context,
        prefab_path: str,
        position_x: float = 0.0,
        position_y: float = 0.0,
        position_z: float = 0.0,
        rotation_x: float = 0.0,
        rotation_y: float = 0.0,
        rotation_z: float = 0.0
    ) -> str:
        """Instantiate a prefab into the current scene at a specified location.

        Args:
            ctx: The MCP context
            prefab_path: Path to the prefab asset (relative to Assets folder)
            position_x: X position in world space (default: 0.0)
            position_y: Y position in world space (default: 0.0)
            position_z: Z position in world space (default: 0.0)
            rotation_x: X rotation in degrees (default: 0.0)
            rotation_y: Y rotation in degrees (default: 0.0)
            rotation_z: Z rotation in degrees (default: 0.0)

        Returns:
            str: Success message or error details
        """
        try:
            unity = get_unity_connection()
            
            # Parameter validation
            if not prefab_path or not isinstance(prefab_path, str):
                return f"Error instantiating prefab: prefab_path must be a valid string"
                
            # Validate numeric parameters
            position_params = {
                "position_x": position_x,
                "position_y": position_y,
                "position_z": position_z,
                "rotation_x": rotation_x,
                "rotation_y": rotation_y,
                "rotation_z": rotation_z
            }
            
            for param_name, param_value in position_params.items():
                if not isinstance(param_value, (int, float)):
                    return f"Error instantiating prefab: {param_name} must be a number"
            
            # Check if the prefab exists
            prefab_dir = '/'.join(prefab_path.split('/')[:-1]) or "Assets"
            prefab_name = prefab_path.split('/')[-1]
            
            # Ensure prefab has .prefab extension for searching
            if not prefab_name.lower().endswith('.prefab'):
                prefab_name = f"{prefab_name}.prefab"
                prefab_path = f"{prefab_path}.prefab"
                
            prefab_assets = unity.send_command("GET_ASSET_LIST", {
                "type": "Prefab",
                "search_pattern": prefab_name,
                "folder": prefab_dir
            }).get("assets", [])
            
            prefab_exists = any(asset.get("path") == prefab_path for asset in prefab_assets)
            if not prefab_exists:
                return f"Prefab '{prefab_path}' not found in the project."
            
            response = unity.send_command("INSTANTIATE_PREFAB", {
                "prefab_path": prefab_path,
                "position_x": position_x,
                "position_y": position_y,
                "position_z": position_z,
                "rotation_x": rotation_x,
                "rotation_y": rotation_y,
                "rotation_z": rotation_z
            })
            
            if not response.get("success", False):
                return f"Error instantiating prefab: {response.get('error', 'Unknown error')} (Path: {prefab_path})"
                
            return f"Prefab instantiated successfully as '{response.get('instance_name', 'unknown')}'"
        except Exception as e:
            return f"Error instantiating prefab: {str(e)} (Path: {prefab_path})"

    @mcp.tool()
    def create_prefab(
        ctx: Context,
        object_name: str,
        prefab_path: str,
        overwrite: bool = False
    ) -> str:
        """Create a new prefab asset from a GameObject in the scene.

        Args:
            ctx: The MCP context
            object_name: Name of the GameObject in the scene to create prefab from
            prefab_path: Path where the prefab should be saved (relative to Assets folder)
            overwrite: Whether to overwrite if a prefab already exists at the path (default: False)

        Returns:
            str: Success message or error details
        """
        try:
            unity = get_unity_connection()
            
            # Parameter validation
            if not object_name or not isinstance(object_name, str):
                return f"Error creating prefab: object_name must be a valid string"
                
            if not prefab_path or not isinstance(prefab_path, str):
                return f"Error creating prefab: prefab_path must be a valid string"
            
            # Check if the GameObject exists
            found_objects = unity.send_command("FIND_OBJECTS_BY_NAME", {
                "name": object_name
            }).get("objects", [])
            
            if not found_objects:
                return f"GameObject '{object_name}' not found in the scene."
                
            # Verify prefab path has proper extension
            if not prefab_path.lower().endswith('.prefab'):
                prefab_path = f"{prefab_path}.prefab"
            
            # Check if a prefab already exists at this path
            prefab_dir = '/'.join(prefab_path.split('/')[:-1]) or "Assets"
            prefab_name = prefab_path.split('/')[-1]
            
            prefab_assets = unity.send_command("GET_ASSET_LIST", {
                "type": "Prefab",
                "search_pattern": prefab_name,
                "folder": prefab_dir
            }).get("assets", [])
            
            prefab_exists = any(asset.get("path") == prefab_path for asset in prefab_assets)
            if prefab_exists and not overwrite:
                return f"Prefab already exists at '{prefab_path}'. Use overwrite=True to replace it."
            
            response = unity.send_command("CREATE_PREFAB", {
                "object_name": object_name,
                "prefab_path": prefab_path,
                "overwrite": overwrite
            })
            
            if not response.get("success", False):
                return f"Error creating prefab: {response.get('error', 'Unknown error')} (Object: {object_name}, Path: {prefab_path})"
                
            return f"Prefab created successfully at {response.get('path', prefab_path)}"
        except Exception as e:
            return f"Error creating prefab: {str(e)} (Object: {object_name}, Path: {prefab_path})"

    @mcp.tool()
    def apply_prefab(
        ctx: Context,
        object_name: str
    ) -> str:
        """Apply changes made to a prefab instance back to the original prefab asset.

        Args:
            ctx: The MCP context
            object_name: Name of the prefab instance in the scene

        Returns:
            str: Success message or error details
        """
        try:
            unity = get_unity_connection()
            
            # Check if the GameObject exists
            found_objects = unity.send_command("FIND_OBJECTS_BY_NAME", {
                "name": object_name
            }).get("objects", [])
            
            if not found_objects:
                return f"GameObject '{object_name}' not found in the scene."
            
            # Check if the object is a prefab instance
            object_props = unity.send_command("GET_OBJECT_PROPERTIES", {
                "name": object_name
            })
            
            # Try to extract prefab status from properties
            is_prefab_instance = object_props.get("isPrefabInstance", False)
            if not is_prefab_instance:
                return f"GameObject '{object_name}' is not a prefab instance."
            
            response = unity.send_command("APPLY_PREFAB", {
                "object_name": object_name
            })
            return response.get("message", "Prefab changes applied successfully")
        except Exception as e:
            return f"Error applying prefab changes: {str(e)}" 

# for importing assets from s3 link
async def download_file(url: str, target_path: str) -> bool:
    """從URL下載文件到指定路徑。

    Args:
        url: 文件的URL
        target_path: 保存文件的本地路徑
        
    Returns:
        bool: 下載是否成功
    """
    try:
        # 確保目標目錄存在
        os.makedirs(os.path.dirname(target_path), exist_ok=True)
        
        # 使用httpx進行下載
        async with httpx.AsyncClient() as client:
            async with client.stream("GET", url) as response:
                response.raise_for_status()
                with open(target_path, 'wb') as f:
                    async for chunk in response.aiter_bytes():
                        f.write(chunk)
        return True
    except Exception as e:
        print(f"下載文件時出錯: {str(e)}")
        return False

def register_remote_asset_tools(mcp: FastMCP):
    """註冊遠程資源工具到MCP服務器。"""
    
    @mcp.tool()
    async def import_remote_asset(
        ctx: Context,
        url: str,
        target_path: str,
        overwrite: bool = False,
        temp_download: bool = True
    ) -> str:
        """從URL下載並導入資源到Unity項目。

        Args:
            ctx: MCP上下文
            url: 要下載的資源URL
            target_path: 資源導入到Unity項目中的路徑（相對於Assets文件夾）
            overwrite: 如果資源已存在是否覆蓋 (default: False)
            temp_download: 是否使用臨時目錄下載文件 (default: True)

        Returns:
            str: 操作結果信息
        """
        try:
            unity = get_unity_connection()
            
            # 參數驗證
            if not url or not isinstance(url, str):
                return f"錯誤: URL必須是有效的字符串"
            
            if not target_path or not isinstance(target_path, str):
                return f"錯誤: 目標路徑必須是有效的字符串"
            
            # 處理目標路徑
            if not target_path.startswith("Assets/"):
                target_path = f"Assets/{target_path}"
            
            # 檢查資源是否已存在
            target_dir = '/'.join(target_path.split('/')[:-1])
            target_filename = target_path.split('/')[-1]
            
            existing_assets = unity.send_command("GET_ASSET_LIST", {
                "search_pattern": target_filename,
                "folder": target_dir or "Assets"
            }).get("assets", [])
            
            # 檢查是否有資源匹配確切路徑
            asset_exists = any(asset.get("path") == target_path for asset in existing_assets)
            if asset_exists and not overwrite:
                return f"資源已存在於'{target_path}'。使用overwrite=True來替換它。"
            
            # 決定下載路徑
            if temp_download:
                # 使用臨時目錄
                temp_dir = tempfile.mkdtemp()
                # 從URL提取文件名
                filename = url.split('/')[-1].split('?')[0]  # 移除查詢參數
                download_path = os.path.join(temp_dir, filename)
            else:
                # 使用與用戶指定的下載路徑
                download_dir = os.path.join(os.path.expanduser("~"), ".unity_mcp_downloads")
                os.makedirs(download_dir, exist_ok=True)
                
                # 從URL提取文件名
                filename = url.split('/')[-1].split('?')[0]  # 移除查詢參數
                download_path = os.path.join(download_dir, filename)
            
            # 下載文件
            download_success = await download_file(url, download_path)
            if not download_success:
                return f"從URL下載資源失敗: {url}"
            
            # 檢查文件是否已下載
            if not os.path.exists(download_path):
                return f"下載似乎成功，但找不到文件: {download_path}"
            
            # 使用Unity導入資源
            response = unity.send_command("IMPORT_ASSET", {
                "source_path": download_path,
                "target_path": target_path,
                "overwrite": overwrite
            })
            
            # 清理臨時文件(如果使用臨時目錄)
            if temp_download:
                try:
                    os.remove(download_path)
                    os.rmdir(temp_dir)
                except:
                    pass  # 忽略清理錯誤
            
            if not response.get("success", False):
                return f"導入資源時發生錯誤: {response.get('error', '未知錯誤')} (URL: {url}, 目標: {target_path})"
                
            return response.get("message", f"資源成功從{url}導入到{target_path}")
        except Exception as e:
            return f"導入遠程資源時發生錯誤: {str(e)}"
    
    @mcp.tool()
    async def batch_import_remote_assets(
        ctx: Context,
        urls: List[str],
        target_folder: str = "Assets/ImportedAssets",
        overwrite: bool = False
    ) -> str:
        """批量從URL列表下載並導入多個資源。

        Args:
            ctx: MCP上下文
            urls: 要下載的資源URL列表
            target_folder: 導入資產的目標文件夾（相對於Assets文件夾）
            overwrite: 如果資源已存在是否覆蓋 (default: False)

        Returns:
            str: 操作結果信息
        """
        if not urls:
            return "URL列表為空，沒有資源需要導入"
        
        results = []
        for i, url in enumerate(urls):
            try:
                # 從URL提取文件名
                filename = url.split('/')[-1].split('?')[0]  # 移除查詢參數
                target_path = f"{target_folder}/{filename}"
                
                # 使用先前定義的工具導入單個資源
                result = await import_remote_asset(
                    ctx, 
                    url=url, 
                    target_path=target_path, 
                    overwrite=overwrite
                )
                
                results.append(f"[{i+1}/{len(urls)}] {filename}: {result}")
            except Exception as e:
                results.append(f"[{i+1}/{len(urls)}] {url}: 錯誤 - {str(e)}")
        
        return "\n".join(results)