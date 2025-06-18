<!-- CONFIRM CAN ID DIALOG -->
        <Grid Grid.Row="1" VerticalOptions="Center" HorizontalOptions="Center">
            <Frame BackgroundColor="Black"
                   Padding="0"
                   CornerRadius="10"
                   WidthRequest="1000"
                   HeightRequest="400"
                   HasShadow="False"
                   BorderColor="Transparent">
                <Grid RowDefinitions="Auto,Auto,Auto" ColumnDefinitions="*" BackgroundColor="LightGray">
                    <!-- White Header Section -->
                    <StackLayout Grid.Row="0" BackgroundColor="White" Padding="40,30" Spacing="10">
                        <Label x:Name="ConfirmText"
                               Text="Set The CAN ID to 195"
                               FontSize="50"
                               FontAttributes="Bold"
                               TextColor="Black"
                               FontFamily="BarlowRegular"
                               HorizontalTextAlignment="Center" />
                    </StackLayout>

                    <!-- Black separator -->
                    <BoxView Grid.Row="1" HeightRequest="10" BackgroundColor="Black" HorizontalOptions="Fill" />

                    <!-- Confirm Button -->
                    <StackLayout Grid.Row="2" VerticalOptions="Center" HorizontalOptions="Center" Margin="0,20,0,0">
                        <Button Text="Confirm"
                                FontFamily="BarlowRegular"
                                BackgroundColor="Black"
                                TextColor="White"
                                FontSize="35"
                                WidthRequest="200"
                                HeightRequest="55"
                                CornerRadius="30"
                                HorizontalOptions="Center"
                                Clicked="OnConfirmClicked" />
                    </StackLayout>
                </Grid>
            </Frame>
        </Grid>
