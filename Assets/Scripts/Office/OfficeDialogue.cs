using System.Collections.Generic;
namespace TheElevator.Office
{
    // What employees mean when they talk. They speak the made-up office language; this text is only ever shown in
    // the speech bubble above their heads, one phrase at a time. Angry lines are rude but never swear.
    public enum Grievance { None, Bump, Shove, Foam, Computer, Award, Property, Thrown, Theft, Noise, Blast }

    public static class OfficeDialogue
    {
        // Anger tiers for choosing lines: annoyed (up to 2), upset (3-4), furious (5 and up), snapped (20 and up).
        public enum Tier { Annoyed, Upset, Furious, Snapped }
        public static Tier TierFor(float anger) { return anger >= OfficeEmployee.SnapPoint ? Tier.Snapped : anger >= 5 ? Tier.Furious : anger >= 3 ? Tier.Upset : Tier.Annoyed; }

        // ---- Hallway small talk: lines alternate between the two colleagues, first speaker first ----
        public static readonly string[][] Conversations = {
            new[] { "Hey, did you finish the quarterly report?", "Almost. The numbers still don't add up.", "They never do. Just round up.", "That's how we got audited last year.", "Okay, fair. I'll bring you a coffee." },
            new[] { "The printer's jammed again.", "Did you try turning it off and on?", "Three times. It just blinks at me.", "I'll put in a ticket with facilities.", "So we'll see it fixed next quarter, then." },
            new[] { "You coming to the team lunch Friday?", "Only if it's not the salad place again.", "I heard they booked the noodle bar.", "Oh, then I'm in. Two bowls." },
            new[] { "How was the budget meeting?", "Two hours. We agreed to schedule another meeting.", "About what?", "About why the first one took two hours." },
            new[] { "Any plans for the weekend?", "Painting the kitchen. My partner picked orange.", "Bold.", "It's called Sunset Mango, apparently.", "You have to send me pictures." },
            new[] { "Is the coffee machine making a weird noise to you?", "Like a whistle or like grinding?", "Like a really sad trombone.", "Oh, that's the descaling alarm. Nobody knows how to fix it." },
            new[] { "Did you take the freight elevator this morning?", "No way. That thing makes me nervous.", "It went down when I pressed up.", "Classic. I'm sticking to the stairs." },
            new[] { "Did you hear Dana got team lead?", "Good for her. She's earned it.", "Does she get the corner desk now?", "And the window. I'm a little jealous, honestly." },
            new[] { "Who's the new facilities assistant?", "No idea. Nobody sent an intro email.", "I saw them carrying a monitor earlier.", "Huh. Probably an upgrade. Finally." },
            new[] { "Someone parked in my spot again.", "The silver hatchback?", "Yes! Every single day.", "Leave a note. A nice one." },
            new[] { "The client moved the deadline to Thursday.", "Wait, this Thursday?", "This Thursday.", "Well, there goes my dentist appointment." },
            new[] { "Who keeps watering the plants?", "They're plastic.", "Oh. That explains why they look so healthy.", "Don't tell whoever's watering them." },
            new[] { "Is the Wi-Fi slow for you too?", "Ever since the upgrade.", "What did they even upgrade?", "The password, I think." },
            new[] { "Happy birthday, by the way!", "Thanks! There's cake in the kitchen.", "What kind?", "Carrot. And yes, it counts as a vegetable." },
            new[] { "Did you watch the game last night?", "Only the second half.", "You missed the best part.", "Don't tell me. I recorded it." },
            new[] { "My laptop updated in the middle of my presentation.", "No! How far along were you?", "Slide two of forty.", "Honestly, that might have been a mercy." },
            new[] { "Are we still doing the step challenge?", "I'm at eleven steps today.", "Eleven thousand?", "Eleven. I parked really close." },
            new[] { "Did facilities fix the air conditioning?", "They turned it up.", "It's freezing in here.", "That's why I brought a blanket." },
            new[] { "I think I replied all by accident.", "To the whole company?", "To the whole company.", "Well, now everyone knows your lunch order." },
            new[] { "Have you seen my stapler?", "The red one?", "Yes, the red one.", "Pretty sure it's in the break room." },
            new[] { "How's the new apartment?", "Great, except the neighbor plays the tuba.", "At night?", "Mostly at seven in the morning." },
            new[] { "Do you know what this acronym means?", "Which one?", "Q-B-R-P.", "No idea. Just nod in the meeting." },
            new[] { "I finally cleared my inbox.", "Seriously? All of it?", "Zero unread.", "Enjoy it. It won't last the hour." },
            new[] { "They're moving our desks again.", "Where to this time?", "Closer to the kitchen.", "Oh, that's actually fantastic news." },
            new[] { "Remember to fill out the survey.", "The one about too many surveys?", "That's the one.", "I'll get to it. Eventually." },
            new[] { "My cat sat on my keyboard during a call.", "Did anyone notice?", "She unmuted me. Mid-sneeze.", "Honestly, that's the best meeting I've heard of." },
            new[] { "Any idea when payday is?", "Friday, I think.", "Thank goodness.", "I'm living on crackers until then." },
            new[] { "Did you try the new vending machine?", "It ate my coin.", "Mine gave me two snacks.", "So it balances out, I guess." },
            new[] { "I'm thinking about learning guitar.", "Nice! Acoustic or electric?", "Whichever one is quieter.", "Your neighbors will appreciate that." },
            new[] { "Did you see the email about casual Friday?", "Is it back?", "Only for socks.", "I'll take it. Fun socks it is." }
        };

        // Walking past each other without stopping.
        public static readonly string[] Greetings = { "Morning!", "Hey there.", "How's it going?", "Hi!", "Oh, hey.", "Afternoon.", "Long day, huh?", "Nice shirt.", "See you at lunch?", "Hey, you!" };

        // ---- Reactions, by what happened and how angry they are ----
        static readonly Dictionary<Grievance, string[][]> reactions = new Dictionary<Grievance, string[][]>
        {
            { Grievance.Bump, new[] {
                new[] { "Oh! Excuse you.", "Whoa, careful there.", "Oops. Watch it.", "Hey, easy!", "Do you mind?", "Ouch. My shoulder." },
                new[] { "Hey! Watch where you're going!", "Seriously? Again?", "Do you walk into everyone?", "That's twice now. Knock it off.", "Look up from your feet!", "What is your problem today?" },
                new[] { "Watch where you're walking, you big oaf!", "Do you even have eyes?", "You nearly knocked me flat, you clumsy lump!", "Stop barging into people!", "One more bump and you'll regret it!" },
                new[] { "That's it! I've had it with you!", "You want to bump into people? Fine!", "You picked the wrong coworker!" } } },
            { Grievance.Shove, new[] {
                new[] { "Hey, hands off.", "Excuse me, no pushing.", "Whoa. Personal space?", "Do not push me." },
                new[] { "Did you just shove me?", "Don't push me around!", "Keep your hands to yourself!", "Push me again, I dare you." },
                new[] { "Don't you dare shove me!", "Hands off, you walking disaster!", "Who raised you, a shopping cart?", "You pushy little weasel!" },
                new[] { "Nobody pushes me! Nobody!", "You want to shove? Let's shove!", "That was the last straw!" } } },
            { Grievance.Foam, new[] {
                new[] { "Hey! What's that foam for?", "Ew! That's cold!" },
                new[] { "Stop spraying that at me!", "There's foam in my hair!", "Is that the fire extinguisher?" },
                new[] { "You sprayed foam in my face!", "I can't see, you absolute clown!", "This was a brand new suit!", "Put that extinguisher down, you menace!", "There's foam in my ears!", "I'm soaked! You're going to pay for dry cleaning!" },
                new[] { "Foam me again. I dare you!", "You think that's funny? Laugh at this!", "I'm done being your target!" } } },
            { Grievance.Computer, new[] {
                new[] { "Hey, I was using that." },
                new[] { "Hey! I was using that to work with!", "Excuse me, that's my computer!" },
                new[] { "Hey! I was using that to work with! Give that back!", "Give me back my computer!", "I had three hours of unsaved work on that!", "Put my computer down right now, you thief!", "You took my computer! Unbelievable!" },
                new[] { "My computer! You're finished!", "Nobody steals my work and walks away!", "I'm getting my computer back one way or another!" } } },
            { Grievance.Award, new[] {
                new[] { "Hey, is that my award?", "Um, that's mine." },
                new[] { "Hey! That's my award!", "Put my trophy back, please.", "I earned that, you know.", "Seriously? My award?", "That's from the company picnic! Give it back!" },
                new[] { "Give me back my award, you trophy thief!", "Employee of the month! Mine! Not yours!", "You'll never earn one of your own, will you?" },
                new[] { "You stole my award! You're done!", "That trophy is going right back where it belongs, and so are you!" } } },
            { Grievance.Property, new[] {
                new[] { "Hey, that's off my desk.", "Um, that's mine." },
                new[] { "Put that back on my desk!", "That's company property. And it's mine.", "Hands off my things!", "Do you usually take stuff off people's desks?" },
                new[] { "Give that back, you sneaky thief!", "Stop taking my things!", "I'm calling security on you!", "Drop it. Now!" },
                new[] { "You keep taking my stuff! That's it!", "Thief! You're not getting away this time!" } } },
            { Grievance.Thrown, new[] {
                new[] { "Ow! Who threw that?" },
                new[] { "Ow! Did you just throw that at me?", "Stop throwing things!" },
                new[] { "You threw that at my head!", "Ow! Are you out of your mind?", "Throw one more thing and see what happens!", "That hurt, you lunatic!", "What is wrong with you? Throwing things at people?" },
                new[] { "Throwing things? I'll show you throwing things!", "That's it! No more mister nice coworker!", "You just made the biggest mistake of your career!" } } },
            { Grievance.Blast, new[] {
                new[] { "Whoa! What was that?" },
                new[] { "My ears are ringing!", "Who is setting off explosions in here?" },
                new[] { "Someone could get hurt! Me! I got hurt!", "This office is a war zone!" },
                new[] { "Everyone's lost their minds!" } } },
            { Grievance.Theft, new[] {
                new[] { "Hey, is that yours?" },
                new[] { "Put that back where you found it!", "That doesn't belong to you." },
                new[] { "Thief! Somebody stop that thief!", "Drop that right now!", "I'm telling security about you!" },
                new[] { "You're not leaving with that!" } } }
        };

        // Furious and chasing: rants about what the player did, mixed with general insults.
        static readonly string[] Insults = {
            "Who even hired you?", "You're the worst temp we've ever had!", "Nobody likes you, you know that?", "Get out of my office!",
            "You have the manners of a potato!", "I'll have your badge for this!", "Honestly, what is wrong with you?", "Come back here!",
            "Don't you walk away from me!", "You're a walking HR complaint!", "I've met printers with better manners!", "You couldn't organize a sock drawer!",
            "I'm writing you up! In triplicate!", "Your performance review is going to be a horror story!" };

        // Cooling off from furious to on edge, after losing sight of the player.
        public static readonly string[] OnEdge = { "Hmph. They got away.", "Fine. But I'm watching you.", "Next time, I won't be so nice.", "Where did they go?", "I'll remember that face.", "Ugh. Back to work, I guess." };
        // Seeing the player again while on edge.
        public static readonly string[] Spotted = { "You again.", "Oh, look who's back.", "Don't even think about it.", "I've got my eye on you.", "Keep walking." };
        // Snapped (20+): drawing a weapon, swinging, shooting, and taking a hit.
        public static readonly string[] DrawPistol = { "That's it! I've had it!", "You want to mess with me? Fine!", "I keep this in my desk for emergencies!", "Consider this your exit interview!" };
        public static readonly string[] DrawBazooka = { "Time for a little restructuring!", "Say goodbye to your career!", "I've been saving this for a special occasion!" };
        public static readonly string[] Swing = { "Take that!", "How do you like that?", "Hold still!", "This is for my coffee!", "Get over here!", "Come on, then!" };
        public static readonly string[] Firing = { "Stay still!", "You can't run forever!", "I'll get you!", "This is what happens!", "Nowhere to hide!" };
        // Bystanders who see someone pull a weapon.
        public static readonly string[] Gasps = { "Gasp!", "Oh no!", "Is that a gun?!", "Everybody get down!", "Oh my gosh!", "What are you doing?!", "Somebody call security!", "Not again!" };
        public static readonly string[] GaspsBazooka = { "Is that a bazooka?!", "Where did that even come from?!", "Oh no, oh no, oh no!", "Everybody run!" };
        // The whole building has had enough.
        public static readonly string[] Riot = { "That's it! Everybody get them!", "We've all had enough of you!", "Get out of our office!", "Somebody grab them!", "You're not getting away this time!" };
        public static readonly string[] Hurt = { "Ow!", "Ouch!", "Hey! That hurt!", "Ugh!", "Oof!" };

        static Tier Clamp(Tier tier, string[][] lines) { return (Tier)System.Math.Min((int)tier, lines.Length - 1); }

        // A reaction to what just happened, never the same line twice in a row for this person.
        public static string Reaction(Grievance grievance, float anger, System.Random random, ref string last)
        {
            if (!reactions.TryGetValue(grievance, out string[][] lines)) lines = reactions[Grievance.Bump];
            return Pick(lines[(int)Clamp(TierFor(anger), lines)], random, ref last);
        }

        // Something to yell while chasing: usually about what the player did, sometimes just an insult.
        public static string Rant(Grievance grievance, float anger, System.Random random, ref string last)
        {
            if (reactions.TryGetValue(grievance, out string[][] lines) && random.NextDouble() < .55)
                return Pick(lines[(int)Clamp(TierFor(anger) >= Tier.Furious ? TierFor(anger) : Tier.Furious, lines)], random, ref last);
            return Pick(Insults, random, ref last);
        }

        public static string Pick(string[] pool, System.Random random, ref string last)
        {
            string line = pool[random.Next(pool.Length)];
            for (int tries = 0; tries < 4 && line == last && pool.Length > 1; tries++) line = pool[random.Next(pool.Length)];
            last = line;
            return line;
        }

        // Splits a line into single phrases; each phrase gets its own bubble.
        public static List<string> Phrases(string line)
        {
            var phrases = new List<string>();
            if (string.IsNullOrEmpty(line)) return phrases;
            int start = 0;
            for (int i = 0; i < line.Length; i++)
            {
                char c = line[i];
                bool end = (c == '.' || c == '!' || c == '?') && (i == line.Length - 1 || line[i + 1] == ' ');
                if (!end) continue;
                string phrase = line.Substring(start, i - start + 1).Trim();
                if (phrase.Length > 0) phrases.Add(phrase);
                start = i + 1;
            }
            if (start < line.Length && line.Substring(start).Trim().Length > 0) phrases.Add(line.Substring(start).Trim());
            return phrases;
        }
    }
}
