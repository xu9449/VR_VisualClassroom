var myImage = document.querySelector('img');

myImage.onclick = function() {
	var mySrc = myImage.getAttribute('src');
	if ( mySrc == 'images/pic1.jpg') {
		myImage.setAttribute ('src', 'images/pic2.jpg');
	} else {
		myImage.setAttribute ('src', 'images/pic1.jpg');
	}
}

var myButton = document.querySelector('button');
myButton.onclick = function() {
  setUserName();
}
var myHeading = document.querySelector('h1');

function setUserName() {
  var myName = prompt('Please enter your name.');
  localStorage.setItem('name', myName);
  myHeading.textContent = 'Mozilla is cool, ' + myName;
}

 if(!localStorage.getItem('name')) {
  setUserName();
} else {
  var storedName = localStorage.getItem('name');
  myHeading.textContent = 'Mozilla is cool, ' + storedName;
}


