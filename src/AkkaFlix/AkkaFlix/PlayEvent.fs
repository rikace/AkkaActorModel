namespace AkkaFlix

    // Every time a user starts streaming a Play-event is sent.
    // User is identified by "User", the video is identified by "Asset".
    type PlayEvent = 
        { User: string
          Asset: string }