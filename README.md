The Class Simulator
A 3D simulation game where players experience and interact within a virtual classroom environment. Built using Unity and C#, this project showcases gameplay mechanics, 3D environment design, and player interactions.

## How to Run

### 1. Open the Project in Unity

1. Launch **Unity Hub**
2. Click **Add** → Select the `Classroom_Project` folder
3. Open the project with Unity 2021.x or compatible version
4. Wait for Unity to import and compile assets (first time may take a few minutes)

### 2. Run the Simulation

**Option A: In Unity Editor (Development)**
1. Open the **Login** scene from `Assets/Scenes/Login.unity`
2. Click the **Play** button in Unity Editor
3. Register a new user or login
4. Select a scenario and start the simulation

**Option B: Build and Run (Standalone)**
1. Go to **File → Build Settings**
2. Select your platform (PC/Mac/Linux or WebGL)
3. Click **Build** and choose output folder
4. Run the generated executable or host the WebGL build

### 3. Backend API

The project connects to a backend API for user authentication and scenarios:
- **URL:** `https://backend-for-project.onrender.com/`
- **Fallback:** If backend is unavailable, scenarios load from `Assets/StreamingAssets/Scenarios/`

> **Note:** The backend may take 30-60 seconds to wake up on first connection (Render.com free tier).

## Default Login (Test)

If you need to test without registration:
- Create a new account using the **Register** button in the Login scene
- Or use local scenarios without backend connection

## Project Structure

```
Classroom_Project/
├── Assets/
│   ├── Scenes/           # Login, MainClassroom, TeacherHome
│   ├── Scripts/          # All C# code
│   ├── Prefabs/          # Student and furniture prefabs
│   └── StreamingAssets/  # JSON scenarios (fallback)
├── Library/              # Unity cache (auto-generated)
└── ProjectSettings/      # Unity configuration
```

## Controls

- **Mouse Click:** Select students
- **UI Buttons:** Teacher actions (praise, yell, call to board, etc.)
- **Camera:** Auto-focus on eager students (configurable)

## Troubleshooting

**Students not spawning?**
- Check that `StudentSpawner` exists in MainClassroom scene
- Verify student prefab is assigned in ClassroomManager

**Backend not connecting?**
- Wait 30-60 seconds for the backend to wake up
- Check Console for connection errors
- System will auto-fallback to local scenarios

**UI not displaying correctly?**
- Ensure TextMesh Pro is imported (should auto-prompt on first run)
- Check that Canvas has EventSystem in the scene

## Authors

- Reemy Halabi
- Michael Trifonov
