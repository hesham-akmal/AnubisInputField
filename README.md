# AnubisInputField
Enhanced Unity input field and keyboard's behavior for Android and iOS.

A modified version of https://github.com/mopsicus/UnityMobileInput

For now it only works with TMP input field as it inherits from TMP_InputField. Basically on selecting an input field, it will not show up the default unity's keyboard, instead it will create an android native EditText out of screen bounds (invisible). Then focus on that edit text which will show up native keyboard (which does not contain unity's keyboard invisible blocking screen ). When adding/removing characters in the invisible edit text, it will sync to unity's input field. The caret position is also synced now. There are more features such as letting keyboard on screen unless manually closed (android/ios).

There's also a menu item "GameObject > Anubis > Replace any Tmp_InputField with AnubisInputField", to convert all TMP_InputFields in the current loaded scene automatically, while copying serialized data of course.

Tested with Editor versions 2019.4.22 and 2020.3

More info and documentation coming soon.
