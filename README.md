# Paw’s Pal

**Paw’s Pal** is an AI-powered Unity application built to help users care for injured or stray animals. It offers instant first aid guidance via image analysis, finds nearby veterinary shelters using location services, and allows users to contact and sign up as volunteers.

## Features

* **AI-Based First Aid**
  Upload an image of the injured animal and describe the injury. The app uses Google Gemini API to generate concise, image-informed first aid advice.

* **Nearby Veterinary Clinics**
  The app fetches a list of nearby shelters and clinics using your GPS location and the Google Places API.

* **Volunteer Connectivity**
  Users can sign up or reach out to volunteers to get assistance or offer help for rescue operations.

* **User-Friendly UI**
  Built with the [Fantasy Wooden GUI](https://assetstore.unity.com/packages/2d/gui/fantasy-wooden-gui-free-103811) from the Unity Asset Store for a charming and intuitive user experience.

## Demo Video

A full demo walkthrough will be available here soon.
**[Watch the demo](https://drive.google.com/drive/folders/17YPrNe3Dq-bSqteT5VB8YXdwPWAgSC9G)**

## How It Works

1. **Upload** an image of the animal in need.
2. **Describe** the injury in a short sentence.
3. **Receive** immediate first aid instructions.
4. **Tap** to find nearby shelters using your current location.
5. **Connect** with volunteers or sign up to become one.

## Technologies Used

* **Unity Engine**
* **Google Gemini API** for AI responses
* **Google Places API** for shelter search
* **Fantasy Wooden GUI** for visual design
* **C# with Unity Web Requests**
* **TextMeshPro** for rich UI text rendering

## Setup Instructions

1. Clone this repository.
2. Open the project in Unity.
3. Replace the placeholder API key fields with your actual Google API key.
4. Build and run on an Android device (location and gallery permissions required).

## Permissions Required

* Location Access
* Storage Access (for image selection)

## Folder Structure

* `Scripts/ChatBot.cs` – Handles AI image + text prompt and response logic.
* `Scripts/Shelters.cs` – Finds and displays nearby clinics.
* `UI/` – All UI prefabs including scrollable lists and chat views.

## License

This project is for educational and non-commercial use only. The Fantasy Wooden GUI asset is used under its own license terms.

