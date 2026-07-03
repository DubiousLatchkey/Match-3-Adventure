use serde::Serialize;
use std::fs;
use std::path::{Component, Path, PathBuf};

#[derive(Serialize)]
struct NativeFileEntry {
    name: String,
    relative_path: String,
}

fn resolve_project_path(root_path: &str, relative_path: &str) -> Result<PathBuf, String> {
    let root = PathBuf::from(root_path);
    if !root.is_absolute() {
        return Err("Project root must be an absolute path.".to_string());
    }

    let relative = Path::new(relative_path);
    if relative.is_absolute()
        || relative.components().any(|component| matches!(component, Component::ParentDir))
    {
        return Err("Path must stay inside the selected project.".to_string());
    }

    Ok(root.join(relative))
}

fn list_files_recursive(
    root_path: &str,
    base_relative_dir: &str,
    current_relative_dir: &str,
    files: &mut Vec<NativeFileEntry>,
) -> Result<(), String> {
    let current_dir = resolve_project_path(root_path, current_relative_dir)?;
    for entry in fs::read_dir(&current_dir).map_err(|error| error.to_string())? {
        let entry = entry.map_err(|error| error.to_string())?;
        let file_type = entry.file_type().map_err(|error| error.to_string())?;
        let name = entry.file_name().to_string_lossy().to_string();
        let relative_path = if current_relative_dir.is_empty() {
            name.clone()
        } else {
            format!("{}/{}", current_relative_dir.replace('\\', "/"), name)
        };

        if file_type.is_dir() {
            list_files_recursive(root_path, base_relative_dir, &relative_path, files)?;
        } else if file_type.is_file() {
            let base_prefix = format!("{}/", base_relative_dir.replace('\\', "/"));
            let display_path = if base_relative_dir.is_empty() {
                relative_path.clone()
            } else {
                relative_path
                    .strip_prefix(&base_prefix)
                    .unwrap_or(&relative_path)
                    .to_string()
            };
            files.push(NativeFileEntry {
                name,
                relative_path: display_path,
            });
        }
    }
    Ok(())
}

#[tauri::command]
fn list_files(root_path: String, relative_dir: String) -> Result<Vec<NativeFileEntry>, String> {
    let mut files = Vec::new();
    list_files_recursive(&root_path, &relative_dir, &relative_dir, &mut files)?;
    files.sort_by(|a, b| a.relative_path.cmp(&b.relative_path));
    Ok(files)
}

#[tauri::command]
fn read_text(root_path: String, relative_path: String) -> Result<String, String> {
    let path = resolve_project_path(&root_path, &relative_path)?;
    fs::read_to_string(path).map_err(|error| error.to_string())
}

#[tauri::command]
fn write_text(root_path: String, relative_path: String, text: String) -> Result<(), String> {
    let path = resolve_project_path(&root_path, &relative_path)?;
    if let Some(parent) = path.parent() {
        fs::create_dir_all(parent).map_err(|error| error.to_string())?;
    }
    fs::write(path, text).map_err(|error| error.to_string())
}

#[tauri::command]
fn remove_file(root_path: String, relative_path: String) -> Result<(), String> {
    let path = resolve_project_path(&root_path, &relative_path)?;
    fs::remove_file(path).map_err(|error| error.to_string())
}

#[cfg_attr(mobile, tauri::mobile_entry_point)]
pub fn run() {
    tauri::Builder::default()
        .plugin(tauri_plugin_dialog::init())
        .invoke_handler(tauri::generate_handler![
            list_files,
            read_text,
            write_text,
            remove_file,
        ])
        .run(tauri::generate_context!())
        .expect("error while running tauri application");
}
