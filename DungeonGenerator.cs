using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class DungeonGenerator : MonoBehaviour {

    private static System.Random randomPercent = new System.Random();
    private static System.Random randomOrientation = new System.Random();

    private static System.Random randomHorVerSplit = new System.Random();
    private static System.Random randomHorDist = new System.Random();
    private static System.Random randomVerDist = new System.Random();

    private const char emptySpace = '0';
    private const char roomSpace = ' ';
    private const char pathSpace = '_';

    private const int SMALL = 1;
    private const int LARGE = 2;


    // Use this for initialization
    void Start() {

        //Modifiable values
        int width = 0;
        int height = 0;
        int roundsOfSplitting = 0;
        int mapSize = LARGE;

        if (mapSize == LARGE)
        {
            width = 75;
            height = 75;
            roundsOfSplitting = 4;
        }
        else if (mapSize == SMALL)
        {
            width = 50;
            height = 50;
            roundsOfSplitting = 3;
        }

        int totalRooms = (int) Mathf.Pow(2, roundsOfSplitting);
        RoomDef[] roomDefs = new RoomDef[totalRooms];
        char[,] map = generateDungeon(width, height);

        //Initialize roomDefs array
        for (int i = 0; i < totalRooms; i++)
        {
            roomDefs[i] = new RoomDef();
        }

        //Create a RoomDef that is the entire map
        roomDefs[0].setNW(0, 0);
        roomDefs[0].setNE(width - 1, 0);
        roomDefs[0].setSW(0, height - 1);
        roomDefs[0].setSE(width - 1, height - 1);

        //Test splitting the room once
        //splitTheRoom(roomDefs[0], roomDefs[1]);

        int filledRooms = 0;
        while (filledRooms < totalRooms - 1)
        {
            int tempRooms = filledRooms;
            for (int i = 0; i <= tempRooms; i++)
            {
                //Debug.Log("Splitting room " + i + " between " + i + " and " + (filledRooms + 1));
                splitTheRoom(roomDefs[i], roomDefs[filledRooms + 1], width, height, roundsOfSplitting);
                //Set each room to be the other's "sister" on the hypothetical binary tree for later path-setting
                roomDefs[i].addSister(filledRooms + 1);
                roomDefs[filledRooms + 1].addSister(i);
                filledRooms++;
            }
        }

        //Make the actual rooms smaller than their sub-sections randomly
        foreach(RoomDef room in roomDefs)
        {
            depleteTheRoom(room);
        }
        
        //Print the rooms to the matrix
        char tile = roomSpace;
        for (int i = 0; i < totalRooms; i++)
        {
            printToMap(map, roomDefs[i], tile);
        }

        //Connect rooms with paths
        for (int i = 0; i < roomDefs.Length; i++)
        {
            int numberOfSisters = roomDefs[i].getSisterCount();
            for (int j = 0; j < numberOfSisters; j++) {
                int sister = roomDefs[i].getSister();
                makePath(map, roomDefs[i], roomDefs[sister], roomDefs);
                roomDefs[sister].removeSister(i);
            }
        }

        //Test resulting map by outputing to textfile
        mapToTextFile(map, width, height);

    }

    // Update is called once per frame
    void Update() {

    }

    //Method to populate the matrix with a random dungeon, where 0s are empty tiles and 1s are floor tiles
    public char[,] generateDungeon(int width, int height)
    {
        char[,] map = new char[height, width];

        for (int i = 0; i < height; i++)
        {
            for (int j = 0; j < width; j++)
            {
                map[i, j] = emptySpace;
            }
        }

        return map;
    }

    //Method to take the passed in room1 and split it between room1 and room2 randomly
    private void splitTheRoom(RoomDef room1, RoomDef room2, int mapWidth, int mapHeight, int roundsOfSplitting)
    {
        const int VERTICAL = 1;
        const int HORIZONTAL = 2;
        int[] originalNE = room1.getNE();
        int[] originalNW = room1.getNW();
        int[] originalSE = room1.getSE();
        int[] originalSW = room1.getSW();

        //Generate a random percent of its original size for room1 to be. room2 will be the remaining percent of the original
        float percent = randomPercent.Next(33, 67);
        //Debug.Log("Random percent = " + percent);

        //And a random value to determine if split is vertical(1) or horizontal(2), then a second random value to decide the side of the split
        int orientation = randomOrientation.Next(1, 3);
        int pickASide = randomOrientation.Next(1, 3);
        //Debug.Log("Random orientation = " + orientation);
        //Debug.Log("Random side = " + pickASide);

        //Check if orientation should be flipped, and if so, flip it
        float roomWidth = room1.getNE()[0] - room1.getNW()[0];
        float roomHeight = room1.getSW()[1] - room1.getNW()[1];
        if (orientation == VERTICAL)
        {
            if (roomHeight > (2 * roomWidth))
            {
                orientation = HORIZONTAL;
            }
        }
        else
        {
            if (roomWidth > 2 * roomHeight)
            {
                orientation = VERTICAL;
            }
        }

        //Use the values to set the new corners
        if ((orientation == VERTICAL) && pickASide == 1)
        {
            //Preserved Corners
            room2.setNE(originalNE[0], originalNE[1]);
            room2.setSE(originalSE[0], originalSE[1]);

            //Calculate new corners
            float width = originalNE[0] - originalNW[0] + 1;
            float calculation = width * (percent / 100);
            int newLeft = (int)Mathf.Round(calculation) + originalNW[0];
            int newRight = newLeft + 1;
            //Debug.Log("width is originalNE " + originalNE[0] + " - originalNW " + originalNW[0]);
            //Debug.Log("width = " + width + ", calculation = " + calculation);
            //Debug.Log("newLeft = " + newLeft + ", newRight = " + newRight);

            //New Corners
            room1.setNE(newLeft, originalNE[1]);
            room1.setSE(newLeft, originalSE[1]);
            room2.setNW(newRight, originalNW[1]);
            room2.setSW(newRight, originalSW[1]);
        }
        else if ((orientation == VERTICAL) && pickASide == 2)
        {
            //Preserved Corners
            room2.setNW(originalNW[0], originalNW[1]);
            room2.setSW(originalSW[0], originalSW[1]);

            //Calculate new corners
            float width = originalNE[0] - originalNW[0] + 1;
            float calculation = width * (percent / 100);
            int newLeft = (int)Mathf.Round(calculation) + originalNW[0];
            int newRight = newLeft + 1;
            //Debug.Log("width is originalNE " + originalNE[0] + " - originalNW " + originalNW[0]);
            //Debug.Log("width = " + width + ", calculation = " + calculation);
            //Debug.Log("newLeft = " + newLeft + ", newRight = " + newRight);

            //New Corners
            room2.setNE(newLeft, originalNE[1]);
            room2.setSE(newLeft, originalSE[1]);
            room1.setNW(newRight, originalNW[1]);
            room1.setSW(newRight, originalSW[1]);
        }
        else if ((orientation == HORIZONTAL) && pickASide == 1)
        {
            //Preserved Corners
            room2.setSW(originalSW[0], originalSW[1]);
            room2.setSE(originalSE[0], originalSE[1]);

            //Calculate new corners
            float height = originalSW[1] - originalNW[1] + 1;
            float calculation = height * (percent / 100);
            int newTop = (int)Mathf.Round(calculation) + originalNW[1];
            int newBottom = newTop + 1;
            //Debug.Log("height is originalSW " + originalSW[1] + " - originalNW " + originalNW[1]);
            //Debug.Log("height = " + height + ", calculation = " + calculation);
            //Debug.Log("newTop = " + newTop + ", newBottom = " + newBottom);

            //New corners
            room1.setSW(originalSW[0], newTop);
            room1.setSE(originalSE[0], newTop);
            room2.setNW(originalNW[0], newBottom);
            room2.setNE(originalNE[0], newBottom);
        }
        else
        {
            //Preserved Corners
            room2.setNW(originalNW[0], originalNW[1]);
            room2.setNE(originalNE[0], originalNE[1]);

            //Calculate new corners
            float height = originalSW[1] - originalNW[1] + 1;
            float calculation = height * (percent / 100);
            int newTop = (int)Mathf.Round(calculation) + originalNW[1];
            int newBottom = newTop + 1;
            //Debug.Log("height is originalSW " + originalSW[1] + " - originalNW " + originalNW[1]);
            //Debug.Log("height = " + height + ", calculation = " + calculation);
            //Debug.Log("newTop = " + newTop + ", newBottom = " + newBottom);

            //New corners
            room2.setSW(originalSW[0], newTop);
            room2.setSE(originalSE[0], newTop);
            room1.setNW(originalNW[0], newBottom);
            room1.setNE(originalNE[0], newBottom);
        }
        
        /*
        //Test print results to debug
        Debug.Log("Room1 NW = " + room1.getNW()[0] + ", " + room1.getNW()[1]);
        Debug.Log("Room1 NE = " + room1.getNE()[0] + ", " + room1.getNE()[1]);
        Debug.Log("Room1 SW = " + room1.getSW()[0] + ", " + room1.getSW()[1]);
        Debug.Log("Room1 SE = " + room1.getSE()[0] + ", " + room1.getSE()[1]);
        Debug.Log("Room2 NW = " + room2.getNW()[0] + ", " + room2.getNW()[1]);
        Debug.Log("Room2 NE = " + room2.getNE()[0] + ", " + room2.getNE()[1]);
        Debug.Log("Room2 SW = " + room2.getSW()[0] + ", " + room2.getSW()[1]);
        Debug.Log("Room2 SE = " + room2.getSE()[0] + ", " + room2.getSE()[1]);
        */

    }

    //Function to make a room only take up a portion of its possible space, to space out the map
    private void depleteTheRoom(RoomDef room)
    {
        //Decide what percent to deplete the room horizontally and vertically by
        float horizontalPercent = randomHorDist.Next(30, 71);
        float verticalPercent = randomVerDist.Next(30, 71);

        //Distribute those percents to the four sides randomly
        float splitPercent = randomHorVerSplit.Next(10, 91);
        float westPercent = verticalPercent * (splitPercent / 100);
        float eastPercent = verticalPercent - westPercent;

        splitPercent = randomHorVerSplit.Next(10, 91);
        float northPercent = horizontalPercent * (splitPercent / 100);
        float southPercent = horizontalPercent - northPercent;

        //Calculate room width and height
        float roomWidth = room.getNE()[0] - room.getNW()[0];
        float roomHeight = room.getSE()[1] - room.getNE()[1];

        //Deplete each side by their percent of the width or height
        float northCalc = roomHeight * (northPercent / 100);
        int northDeplete = Mathf.RoundToInt(northCalc);
        room.setNE(room.getNE()[0], room.getNE()[1] + northDeplete);
        room.setNW(room.getNW()[0], room.getNW()[1] + northDeplete);

        float southCalc = roomHeight * (southPercent / 100);
        int southDeplete = Mathf.RoundToInt(southCalc);
        room.setSE(room.getSE()[0], room.getSE()[1] - southDeplete);
        room.setSW(room.getSW()[0], room.getSW()[1] - southDeplete);

        float westCalc = roomWidth * (westPercent / 100);
        int westDeplete = Mathf.RoundToInt(westCalc);
        room.setNW(room.getNW()[0] + westDeplete, room.getNW()[1]);
        room.setSW(room.getSW()[0] + westDeplete, room.getSW()[1]);

        float eastCalc = roomWidth * (eastPercent / 100);
        int eastDeplete = Mathf.RoundToInt(eastCalc);
        room.setNE(room.getNE()[0] - eastDeplete, room.getNE()[1]);
        room.setSE(room.getSE()[0] - eastDeplete, room.getSE()[1]);

        /*
        Debug.Log("northPercent = " + northPercent + "%");
        Debug.Log("southPercent = " + southPercent + "%");
        Debug.Log("eastPercent = " + eastPercent + "%");
        Debug.Log("westPercent = " + westPercent + "%");
        */

    }

    //Function to create paths between two rooms
    private void makePath(char[,] map, RoomDef room1, RoomDef room2, RoomDef[] roomDefs)
    {
        //Debug.Log("makePath() was called");
        List<int> roomsPassed = new List<int>();
        if (room1.getCenter()[1] < room2.getCenter()[1])
        {
            //Room 1's center is North of Room 2's
            if (room1.getCenter()[0] > room2.getCenter()[0])
            {
                //Debug.Log("Room 1's center is North East of Room 2's");
                walkWest(map, room1.getCenter(), room2.getCenter(), roomsPassed, roomDefs);
                int[] current = { room2.getCenter()[0], room1.getCenter()[1] };
                walkSouth(map, current, room2.getCenter(), roomsPassed, roomDefs);
            }
            else if (room1.getCenter()[0] == room2.getCenter()[0])
            {
                //Debug.Log("Room 1's center is due North of Room 2's");
                walkSouth(map, room1.getCenter(), room2.getCenter(), roomsPassed, roomDefs);
            }
            else
            {
                //Debug.Log("Room 1's center is North West of Room 2's");
                walkEast(map, room1.getCenter(), room2.getCenter(), roomsPassed, roomDefs);
                int[] current = { room2.getCenter()[0], room1.getCenter()[1] };
                walkSouth(map, current, room2.getCenter(), roomsPassed, roomDefs);
            }
        }
        else if (room1.getCenter()[1] == room2.getCenter()[1])
        {
            //Room 1's center is lined up vertically with Room 2's
            if (room1.getCenter()[0] > room2.getCenter()[0])
            {
                //Debug.Log("Room 1's center is due East of Room 2's");
                walkWest(map, room1.getCenter(), room2.getCenter(), roomsPassed, roomDefs);
            }
            else
            {
                //Debug.Log("Room 1's center is due West of Room 2's");
                walkEast(map, room1.getCenter(), room2.getCenter(), roomsPassed, roomDefs);
            }
        }
        else
        {
            //Room 1's center is South of Room 2's
            if (room1.getCenter()[0] > room2.getCenter()[0])
            {
                //Debug.Log("Room 1's center is South East of Room 2's");
                walkNorth(map, room1.getCenter(), room2.getCenter(), roomsPassed, roomDefs);
                int[] current = { room1.getCenter()[0], room2.getCenter()[1] };
                walkWest(map, current, room2.getCenter(), roomsPassed, roomDefs);
            }
            else if (room1.getCenter()[0] == room2.getCenter()[0])
            {
                //Debug.Log("Room 1's center is due South of Room 2's");
                walkNorth(map, room1.getCenter(), room2.getCenter(), roomsPassed, roomDefs);
            }
            else
            {
                //Debug.Log("Room 1's center is South West of Room 2's");
                walkNorth(map, room1.getCenter(), room2.getCenter(), roomsPassed, roomDefs);
                int[] current = { room1.getCenter()[0], room2.getCenter()[1] };
                walkEast(map, current, room2.getCenter(), roomsPassed, roomDefs);
            }
        }

        foreach (int passedRoom in roomsPassed)
        {
            room1.removeSister(passedRoom);
            room2.removeSister(passedRoom);
        }

    }

    private void walkNorth(char[,] map, int[] fromHere, int[] toHere, List<int> roomsPassed, RoomDef[] roomDefs)
    {
        //Debug.Log("walkNorth() called.");
        int[] location = {fromHere[0], fromHere[1]};
        while (location[1] >= toHere[1])
        {
            //Debug.Log("Stepped north.");
            if (map[location[0], location[1]] != roomSpace)
            {
                map[location[0], location[1]] = pathSpace;
                //Debug.Log("Drawing path space at " + location[0] + ", " + location[1]);
            }
            else
            {
                for (int i = 0; i < roomDefs.Length; i++)
                {
                    if (roomDefs[i].isInRoom(location))
                    {
                        roomsPassed.Add(i);
                    }
                }
            }
            location[1]--;
        }
    }

    private void walkSouth(char[,] map, int[] fromHere, int[] toHere, List<int> roomsPassed, RoomDef[] roomDefs)
    {
        //Debug.Log("walkSouth() called.");
        int[] location = { fromHere[0], fromHere[1] };
        while (location[1] <= toHere[1])
        {
            //Debug.Log("Stepped south.");
            if (map[location[0], location[1]] != roomSpace)
            {
                map[location[0], location[1]] = pathSpace;
                //Debug.Log("Drawing path space at " + location[0] + ", " + location[1]);
            }
            else
            {
                for (int i = 0; i < roomDefs.Length; i++)
                {
                    if (roomDefs[i].isInRoom(location))
                    {
                        roomsPassed.Add(i);
                    }
                }
            }
            location[1]++;
        }
    }

    private void walkEast(char[,] map, int[] fromHere, int[] toHere, List<int> roomsPassed, RoomDef[] roomDefs)
    {
        //Debug.Log("walkEast() called.");
        int[] location = { fromHere[0], fromHere[1] };
        while (location[0] <= toHere[0])
        {
            //Debug.Log("Stepped east.");
            if (map[location[0], location[1]] != roomSpace)
            {
                map[location[0], location[1]] = pathSpace;
                //Debug.Log("Drawing path space at " + location[0] + ", " + location[1]);
            }
            else
            {
                for (int i = 0; i < roomDefs.Length; i++)
                {
                    if (roomDefs[i].isInRoom(location))
                    {
                        roomsPassed.Add(i);
                    }
                }
            }
            location[0]++;
        }
    }

    private void walkWest(char[,] map, int[] fromHere, int[] toHere, List<int> roomsPassed, RoomDef[] roomDefs)
    {
        //Debug.Log("walkWest() called.");
        int[] location = { fromHere[0], fromHere[1] };
        while (location[0] >= toHere[0])
        {
            //Debug.Log("Stepped west.");
            if (map[location[0], location[1]] != roomSpace)
            {
                map[location[0], location[1]] = pathSpace;
                //Debug.Log("Drawing path space at " + location[0] + ", " + location[1]);
            }
            else
            {
                for (int i = 0; i < roomDefs.Length; i++)
                {
                    if (roomDefs[i].isInRoom(location))
                    {
                        roomsPassed.Add(i);
                    }
                }
            }
            location[0]--;
        }
    }

    private void printToMap(char[,] map, RoomDef room, char tile)
    {
        for (int j = room.getNW()[1]; j <= room.getSW()[1]; j++)
        {
            for (int i = room.getNW()[0]; i <= room.getNE()[0]; i++)
            {
                //Debug.Log("i = " + i + ", j = " + j);
                map[i, j] = tile; //TODO Array index out of bounds exception
            }
        }
    }

    private void mapToTextFile(char[,] map, int width, int height)
    {
        string[] lines = new string[height];
        for (int j = 0; j < height; j++)
        {
            for (int i = 0; i < width; i++)
            {
                lines[j] += map[i, j] + " ";
            }

            using (System.IO.StreamWriter file =
                new System.IO.StreamWriter("test_map_file.txt"))
            {
                foreach (string line in lines)
                {
                    file.WriteLine(line);
                }
            }

        }

    }

    //Class to store the 4 corners of a room
    private class RoomDef
    {
        private int[] NE = new int[2];
        private int[] NW = new int[2];
        private int[] SE = new int[2];
        private int[] SW = new int[2];
        private List<int> sisters = new List<int>();

        public RoomDef()
        {
            NE[0] = 0;
            NE[1] = 0;
            NW[0] = 0;
            NW[1] = 0;
            SE[0] = 0;
            SE[1] = 0;
            SW[0] = 0;
            SW[1] = 0;
        }


        public void setNE(int x, int y)
        {
            NE[0] = x;
            NE[1] = y;
        }

        public void setNW(int x, int y)
        {
            NW[0] = x;
            NW[1] = y;
        }

        public void setSE(int x, int y)
        {
            SE[0] = x;
            SE[1] = y;
        }

        public void setSW(int x, int y)
        {
            SW[0] = x;
            SW[1] = y;
        }

        public void addSister(int sister)
        {
            sisters.Add(sister);
        }

        public int[] getNE()
        {
            return NE;
        }

        public int[] getNW()
        {
            return NW;
        }

        public int[] getSE()
        {
            return SE;
        }

        public int[] getSW()
        {
            return SW;
        }

        public int getSister()
        {
            if (sisters.Count > 0)
            {
                int value = sisters[sisters.Count - 1];
                sisters.RemoveAt(sisters.Count - 1);
                return value;
            }
            else
            {
                return -1;
            }
        }

        public int getSisterCount()
        {
            return sisters.Count;
        }

        public void removeSister(int removeMe)
        {
            sisters.Remove(removeMe);
        }

        public bool hasSister()
        {
            return (sisters.Count > 0);
        }

        public int[] getCenter()
        {
            int[] center = new int[2];

            center[0] = (NE[0] + NW[0]) / 2;
            center[1] = (SW[1] + NW[1]) / 2;

            return center;
        }

        public int[] getRandomSpot()
        {
            int[] spot = new int[2];
            int randomX = randomHorDist.Next(NE[0], (NW[0] + 1));
            int randomY = randomVerDist.Next(NW[1], (SW[1] + 1));
            return spot;
        }

        public bool isInRoom(int[] point)
        {
            bool isInRoom = false;
            int x = point[0];
            int y = point[1];
            if ((x < NE[0]) && (x > NW[0]))
            {
                if ((y < SW[1]) && (y > NW[1]))
                {
                    isInRoom = true;
                }
            }
            return isInRoom;
        }
    }

}
