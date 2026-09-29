namespace TheElevator.Office
{
    // What employees mean when they talk. They speak the made-up office language; this text is only ever shown
    // in the speech bubble above their heads. Angry lines are rude but never swear.
    public enum Grievance { None, Bump, Shove, Foam, Computer, Theft, Noise }

    public static class OfficeDialogue
    {
        // Hallway small talk between two colleagues; lines alternate, first speaker first.
        public static readonly string[][] Conversations = {
            new[] { "Did you finish the quarterly report?", "Almost. The numbers still don't add up.", "They never do. Just round them up.", "That's how we ended up with an audit last year.", "Fair point. I'll bring you a coffee." },
            new[] { "The printer on this floor is jammed again.", "Did you try turning it off and on?", "Three times. It just blinks at me.", "I'll put in a ticket with facilities.", "They'll get to it by next quarter." },
            new[] { "Are you coming to the team lunch on Friday?", "Only if it's not the salad place again.", "I heard they booked the noodle bar.", "Then count me in. Two bowls." },
            new[] { "How was the budget meeting?", "Two hours. We agreed to schedule another meeting.", "About what?", "About why the first one took two hours." },
            new[] { "Any plans for the weekend?", "Painting the kitchen. My partner picked orange.", "Bold choice.", "It's called Sunset Mango, apparently.", "Send me pictures." },
            new[] { "The coffee machine is making a new noise.", "Like a whistle or like grinding?", "Like a sad trombone.", "That's the descaling alarm. Nobody knows how to fix it." },
            new[] { "Did you take the freight elevator this morning?", "No, it makes me nervous.", "It went down when I pressed up.", "Classic. I'm sticking to the stairs." },
            new[] { "I heard Dana got the team lead job.", "Good for her. She earned it.", "Does that mean she gets the corner desk?", "And the window. I'm a little jealous." },
            new[] { "Who's the new facilities assistant?", "No idea. Nobody sent an intro email.", "I saw them carrying a monitor earlier.", "Probably an upgrade. Finally." },
            new[] { "Someone parked in my spot again.", "The silver hatchback?", "Yes! Every single day.", "Leave a note. A polite one." },
            new[] { "The client moved the deadline to Thursday.", "This Thursday?", "This Thursday.", "Then I'm cancelling my dentist appointment." },
            new[] { "Who keeps watering the plants?", "They're plastic.", "That explains why they look so healthy.", "Don't tell whoever is watering them." },
            new[] { "Is the Wi-Fi slow for you too?", "It's been slow since the upgrade.", "What did they upgrade?", "The password, I think." },
            new[] { "Happy birthday, by the way!", "Thanks! There's cake in the kitchen.", "What kind?", "Carrot. Before you ask, yes, it counts as a vegetable." }
        };

        // Mild annoyance: anger levels 1 and 2.
        public static readonly string[] Annoyed = { "Oh! Excuse you.", "Careful there.", "Hey, watch it.", "Do you mind?" };

        // Moderately upset: anger levels 3 and 4.
        public static readonly string[] Upset = { "Hey! Watch where you're going!", "Seriously, knock it off.", "That's not okay.", "Could you please be more careful?", "What is your problem today?", "I'm getting really tired of this." };

        // Furious (level 5): rude, specific to what the player did.
        static readonly string[] BumpRage = { "Watch where you're walking, you big oaf!", "Do you even have eyes?", "You nearly knocked me over, you clumsy lump!", "Stop barging into people!" };
        static readonly string[] ShoveRage = { "Don't you dare shove me!", "Hands off, you walking disaster!", "Push me again and see what happens!", "Who raised you, a shopping cart?" };
        static readonly string[] FoamRage = { "You sprayed foam in my face!", "I can't see, you absolute clown!", "This was a brand new suit!", "Put that extinguisher down, you menace!", "There's foam in my ears!" };
        static readonly string[] ComputerRage = { "You took my computer, you sneaky thief!", "Put my computer back right now!", "I had three hours of unsaved work on that!", "Give it back, you little weasel!" };
        static readonly string[] TheftRage = { "That doesn't belong to you!", "I'm telling security about you!", "Thief! Somebody stop this thief!", "Drop that right now!" };
        static readonly string[] GeneralRage = { "Who even hired you?", "You're the worst temp we've ever had!", "Nobody likes you, you know that?", "Get out of my office!", "You have the manners of a potato!", "I'll have your badge for this!", "Honestly, what is wrong with you?" };

        public static string Rage(Grievance grievance, System.Random random)
        {
            string[] specific = grievance == Grievance.Bump ? BumpRage : grievance == Grievance.Shove ? ShoveRage : grievance == Grievance.Foam ? FoamRage
                : grievance == Grievance.Computer ? ComputerRage : grievance == Grievance.Theft ? TheftRage : null;
            // Mostly about what happened, sometimes just general insults.
            if (specific != null && random.NextDouble() < .65) return specific[random.Next(specific.Length)];
            return GeneralRage[random.Next(GeneralRage.Length)];
        }

        public static string ReactionFor(int anger, Grievance grievance, System.Random random)
        {
            if (anger >= 5) return Rage(grievance, random);
            if (anger >= 3) return Upset[random.Next(Upset.Length)];
            return Annoyed[random.Next(Annoyed.Length)];
        }
    }
}
