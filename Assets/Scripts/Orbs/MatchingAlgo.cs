﻿using System;
using System.Collections.Generic;
using System.Linq;

namespace Assets.Scripts.Orbs {

    /// <summary>
    /// Algorithm for matching orbs
    /// </summary>
    public static class MatchingAlgo {

        /// <summary>
        /// Matches any orbs within a 2D array of Orbs
        /// </summary>
        /// <param name="orbs">2D array of Orb instance</param>
        /// <returns>List that includes lists of matched orb instance</returns>
        public static List<List<Orb>> matchOrb(Orb[,] orbs) {
            List<List<Orb>> matchedList = new List<List<Orb>>();
            // Loop through the rows first to find any connected 3 orbs on the rows
            for (int i=0; i<orbs.GetLength(0); i++) {
                // Create an Orb[] that only contains 1 single row of orb
                Orb[] orbOnRow = new Orb[6];
                for (int j=0; j<6; j++) {
                    orbOnRow[j] = orbs[i, j];
                }
                // Call processor function
                matchedList = matchedList.Concat(matchRowColumn(orbOnRow, new List<Orb>(), new List<List<Orb>>())).ToList();
            }
            // Loop through all columns to find any connected 3 orbs on the columns
            for (int i = 0; i < orbs.GetLength(1); i++) {
                // Create an Orb[] that only contains 1 single column of orb
                Orb[] orbOnColumn = new Orb[5];
                for (int j = 0; j < 5; j++) {
                    orbOnColumn[j] = orbs[j, i];
                }
                // Call processor function
                matchedList = matchedList.Concat(matchRowColumn(orbOnColumn, new List<Orb>(), new List<List<Orb>>())).ToList();
            }
            // Merges possible match that are spanning across both rows and columns
            matchedList = connectDups(matchedList);
            return matchedList;
        }

        /// <summary>
        /// Matches any orbs within a 1D array of Orbs
        /// </summary>
        /// <param name="orbs">1D array of Orb instance that are on the same row or column</param>
        /// <param name="currentlyMatching">List of Orb instance that we are currently matching, either have not reaches 3 orbs yet, or have reached 3 orbs but is not terminated by another orb with a different color</param>
        /// <param name="matched">List of list of Orb instance that has already been matched</param>
        /// <returns>List that includes lists of matched orb instance along a row or column</returns>
        private static List<List<Orb>> matchRowColumn(Orb[] orbs, List<Orb> currentlyMatching, List<List<Orb>> matched) {
            Orb currentOrb = orbs[0];
            if (currentlyMatching.Count == 0 || currentOrb.getType() == currentlyMatching[0].getType()) {
                // Continue matching as the orb has the same type as those already in the list
                currentlyMatching.Add(currentOrb);
            }
            else {
                // Match terminated here
                if (currentlyMatching.Count >= 3) {
                    // Matched 3 orbs or more --> Successfully matched
                    // Push it into the matched arraylist
                    // A new list is created since we are about to Clear this list, and Add() only add a memory reference that will also be wiped
                    matched.Add(new List<Orb>(currentlyMatching)); 
                }
                // Clear existing matching list
                currentlyMatching.Clear();
                // Initiate new matching
                currentlyMatching.Add(currentOrb);
            }
            if (orbs.Length == 1) {
                if (currentlyMatching.Count >= 3) {
                    // Matched 3 orbs or more --> Successfully matched
                    // Push it into the matched arraylist
                    matched.Add(new List<Orb>(currentlyMatching));
                }
                // Matched the entire row / column already
                return matched;
            }
            else {
                // Continue matching
                // Create a new Orb[] but without the processed array
                Orb[] truncatedOrbs = new Orb[orbs.Length - 1];
                Array.Copy(orbs, 1, truncatedOrbs, 0, truncatedOrbs.Length);
                // Call this method again with the new list and shortened array
                return matchRowColumn(truncatedOrbs, currentlyMatching, matched);
            }
        }

        /// <summary>
        /// Connect any duplicates match in rawMatches from matchRowColumn for match that span across both row and column
        /// </summary>
        /// <note>
        /// For example, if there is a match from [0,1] [0,2] [0,3] [1,1] [2,1], the matchRowColumn will detect it as 2 separate entries [0,1] [0,2] [0,3] and [0,1] [1,1], [2,1]
        /// This method connects the 2 match via a common memeber [0,1]
        /// </note>
        /// <param name="rawMatches">Raw matches returned from matchRowColumn</param>
        /// <returns>Processed List with duplicates connected</returns>
        private static List<List<Orb>> connectDups(List<List<Orb>> rawMatches) {
            // Instantiate a Dictionary that store the index that list that each Orb instance belongs it
            // For example, if Orb A belongs to List 1 in rawMatches, then the record would be { Orb A --> 1 }
            Dictionary<Orb, int> uniqueDict = new Dictionary<Orb, int>();
            // Instantiate a List for storing index that has to be merged
            // For example, if index 1 and 2 has to be merged, then pendingMerge = { 1, 2, ...etc. }
            List<int> pendingMerge = new List<int>();
            // Loop through every Orb instance to look for possible merges
            foreach (List<Orb> matchedRowColumn in rawMatches) {
                int currentIndex = rawMatches.IndexOf(matchedRowColumn);
                foreach (Orb orb in matchedRowColumn) {
                    // Check if the Orb has already been registered at the Dictionary
                    if (uniqueDict.ContainsKey(orb)) {
                        // Merges is required since there are more than 1 match that contain the same Orb
                        pendingMerge.Add(uniqueDict[orb]);
                        pendingMerge.Add(currentIndex);
                    }
                    else {
                        // No merge required, just register at the Dictionary
                        uniqueDict.Add(orb, currentIndex);
                    }
                }
            }
            // Instantiate a new List to store processed List
            List<List<Orb>> processedMatches = new List<List<Orb>>();
            // Loop through rawMatches
            for (int i=0; i<rawMatches.Count; i++) {
                // Check if the current list has to be merged with other list
                int mergeIndex = pendingMerge.IndexOf(i);
                if (mergeIndex == -1) {
                    // No merges required
                    processedMatches.Add(rawMatches[i]);
                }
            }
            // Add the records that require merges
            processedMatches = processedMatches.Concat(mergeMatches(pendingMerge, rawMatches)).ToList();
            // Done
            return processedMatches;
        }

        /// <summary>
        /// Merges matches in rawMatches in accordance to pendingMerge
        /// </summary>
        /// <param name="pendingMerge">1D int array that describe which matches should be merged, entries should be written 2 as a pair like 1,2 for merging match 1 and match 2</param>
        /// <param name="rawMatches">Raw matches returned from matchRowColumn</param>
        /// <returns></returns>
        private static List<List<Orb>> mergeMatches(List<int> pendingMerge, List<List<Orb>> rawMatches) {
            // Instantiate a new List to store merged List
            List<List<Orb>> mergedMatches = new List<List<Orb>>();
            // Loop through pendingMerge
            while (pendingMerge.Count > 0) {
                // Exclude those already merged which will be set to -1
                List<int> connectedMatch = new List<int>();
                // Add the 2 immediate record first, i and i+1
                // For example, imagine an array that looks like this [1,2, 2,3, 1,4, 5,6], then we are adding 1, 2 to out connectedMatch first
                connectedMatch.Add(pendingMerge[0]);
                connectedMatch.Add(pendingMerge[1]);
                // Remove the processed match from pendingMerge
                // We are removing index 0 for twice since the list is automatically renumbered once the first element is removed where the second element would then be numbered at 0
                // Example array would then become [2,3, 1,4, 5,6]
                pendingMerge.RemoveAt(0);
                pendingMerge.RemoveAt(0);
                // Loop through each entry of connectedMath to locate any clustered merge request (e.g. 1 --> 2 --> 3 --> 4 ...etc.)
                for (int j=0; j<connectedMatch.Count; j++) {
                    while (true) {
                        // Attempts to find additional merge request for a raw match
                        // For instance, we want to locate 2 is connected 3, and 1 is connected to 4 in the example array
                        // This while loop will find all merge request that is related to connectedMatch[j]
                        int nextEntry = pendingMerge.IndexOf(connectedMatch[j]);
                        if (nextEntry != -1) {
                            if (nextEntry % 2 == 0) {
                                // If % 2 == 0, it indicate the detected entry is a even entry, where its partner is after its index
                                // Additional merge request found, adds it to connectedMatch
                                connectedMatch.Add(pendingMerge[nextEntry + 1]);
                                // Remove the processed match from pendingMerge
                                pendingMerge.RemoveAt(nextEntry);
                                pendingMerge.RemoveAt(nextEntry);
                            }
                            else {
                                // If % 2 == 0, it indicate the detected entry is an odd entry, where its partner is before its index
                                connectedMatch.Add(pendingMerge[nextEntry - 1]);
                                pendingMerge.RemoveAt(nextEntry - 1);
                                pendingMerge.RemoveAt(nextEntry - 1);
                            }
                        }
                        else {
                            // No more merge request related to connectedMatch[j]
                            break;
                        }
                    }
                    // Look for merge request related to connectedMatch[j + 1], until every single match has gone through the while loop
                }
                // Merges the required matches
                // Instantiate a list to store the merged result
                List<Orb> mergedMatch = new List<Orb>();
                // Loop through each match list that has to be merged
                foreach (int matchIndex in connectedMatch) {
                    // Loop through each Orb in each list
                    foreach (Orb orb in rawMatches[matchIndex]) {
                        // Adds it to mergedMatch if not already exist
                        if (!mergedMatch.Contains(orb)) {
                            mergedMatch.Add(orb);
                        }
                    }
                }
                // Add the mergedMatch to the master List<List<Orb>>
                mergedMatches.Add(mergedMatch);
            }
            // Done
            return mergedMatches;
        }

    }

}
