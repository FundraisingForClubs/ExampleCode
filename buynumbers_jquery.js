$(document).ready(function () {

    var maxNumbersPerPlayer = $("#NumbersRemaining").val();
    var maxNumbers = 100;
    var numberPrice = $("#NumberPrice").val();
    var memberNumbers = [0, 0, 0, 0, 0];

    $("table span").on("click", function () {
        var selectedNumber = $(this).text();
        //alert("You clicked " + selectedNumber);

        if ($("#Number_" + selectedNumber).hasClass("NumberTaken")) {
            //alert("Unselect Number");
            // Unselect Number
            $("#Number_" + selectedNumber).removeClass("NumberTaken");
            UnSelectNumber(selectedNumber);
        }
        else {
            AddSelectedNumber(selectedNumber);
        }

        updateTotals();

    });

    $("#btnSelectRandomNumber").on("click", function () {

        var numberFound = false;

        while (!numberFound) {

            var randomNumber = Math.floor(Math.random() * 100) + 1;
            //Check that number has not been taken
            if (!$("#Number_" + randomNumber).hasClass("NumberTaken")) {
                //Select Number
                AddSelectedNumber(randomNumber);
                break;
            }
        }

        updateTotals();
    });

    function AddSelectedNumber(selectedNumber) {
        for (var i = 0; i <= maxNumbersPerPlayer; i++) {
            //alert(i + " = " + memberNumbers[i]);
            if (memberNumbers[i] == 0) {
                memberNumbers[i] = selectedNumber;
                $("#MemberNumber_" + i).text(selectedNumber);
                $("#MemberNumber_" + i).addClass("MemberNumbers");

                $("#Number_" + selectedNumber).addClass("NumberTaken");
                //$("#Number_" + selectedNumber).addClass("MemberSelected");
                break;
            }
        }
    }

    function UnSelectNumber(selectedNumber) {

        var tempList = [0, 0, 0, 0, 0];
        var tempIndex = 0;

        for (var i = 0; i < maxNumbersPerPlayer; i++) {
            //alert("Index: " + i + " = " + memberNumbers[i])
            if (memberNumbers[i] != selectedNumber) {
                //Add to Temp List
                tempList[tempIndex] = memberNumbers[i];
                tempIndex++;
            }
        }

        //tempList.forEach((element) => alert(element)); 

        memberNumbers = tempList;

        // Display Selected Numbers
        for (var i = 0; i < maxNumbersPerPlayer; i++) {
            if (memberNumbers[i] > 0) {
                $("#MemberNumber_" + i).text(memberNumbers[i]);
            }
            else {
                $("#MemberNumber_" + i).text("");
                $("#MemberNumber_" + i).removeClass("MemberNumbers");
            }
        }
    }

    function updateTotals() {
        var numberCount = 0;
        var newNumbers = "";

        $("#NewClubTickets").val();


        for (var i = 0; i < maxNumbersPerPlayer; i++) {
            if (memberNumbers[i] > 0) {
                newNumbers = newNumbers + "," + memberNumbers[i]
                numberCount++;
            }
        }
        if (numberCount > 0) {
            $("#NewClubTickets").val(newNumbers.substring(1));
        }

        $("#memberNumberCount").text(numberCount);

        var monthlyCharge = numberCount * numberPrice

        $("#monthlyCharge").text(monthlyCharge);
    }
});