   M S B A M L   `   `   `  ����\  XHelixToolkit.Wpf, Version=2015.1.715.0, Culture=neutral, PublicKeyToken=52aa3500039caf0dM8clr-namespace:HelixToolkit.Wpf;assembly=HelixToolkit.WpfHelixToolkit.Wpf  5   	   A =Avatar, Version=1.0.0.0, Culture=neutral, PublicKeyToken=nullclr-namespace:AvatarAvatar 5   	      Avatar.MainWindow   5   	   W SPresentationCore, Version=4.0.0.0, Culture=neutral, PublicKeyToken=31bf3856ad364e35R NWindowsBase, Version=4.0.0.0, Culture=neutral, PublicKeyToken=31bf3856ad364e35\ XPresentationFramework, Version=4.0.0.0, Culture=neutral, PublicKeyToken=31bf3856ad364e35D 9http://schemas.microsoft.com/winfx/2006/xaml/presentation    5   	   R NSystem.Xaml, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089:x,http://schemas.microsoft.com/winfx/2006/xaml     5   	   @d2http://schemas.microsoft.com/expression/blend/2008     5   	   Jmc;http://schemas.openxmlformats.org/markup-compatibility/2006     5   	   QHelixToolkit8clr-namespace:HelixToolkit.Wpf;assembly=HelixToolkit.Wpf     5   	   &localclr-namespace:Avatar     5   	   -   5          � Title$  ImperialAvatar��5	   	   $
��1000��6    $
��1000��6.   .��5      � .+�5   
   �� -   5        $��start_button��5   5   $��0,0,742,879q�6    .��5      ��  �� Orientation$ 
Horizontal=�6   .+�5      �� $��Start��6   5      5      �� 5   
   -   5        $��connection_button��5   !   $��0,130,589,676.5q�6:   .��5      �� $ 
Horizontal=�6   .+�5      �� $��Connect��6   5      5      �� 5   
   -   5         ��Name$ stream_button��5       $��621,130,0,676.5q�67   .��5      �� $ 
Horizontal=�6   .+�5      �� $��Stream��6   5      5      �� 5   
   -   5         ��Name$ Avatar��5      $��0q�6"   $��Stretch=�66   $��Stretch=�6T   $��0,90,0,0q�6p   ._�5      &    HelixToolkit.Wpf.HelixViewport3D  -   5        $��m_helix_viewport��5   �    J� IsManipulationEnabled .     IsRotationEnabled .     IsMoveEnabled .     IsPanEnabled .     ZoomExtentsWhenLoaded . $��-20,91,20,20q�6�   	   Children.	 5      $   HelixToolkit.Wpf.DefaultLights  g� 5!      
 g� Content
 6    �� *
skeleton+5"      5#      5%      5&      